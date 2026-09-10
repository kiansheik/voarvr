using System;
using System.Linq;
using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Flight
{
    public sealed class AvianWingPresentation : MonoBehaviour, IWingArticulation
    {
        private struct Feather
        {
            public Transform Bone;
            public Quaternion Rest;
            public Vector3 FanAxis, PitchAxis;
        }
        private Feather[] leftPrimary,rightPrimary,leftSecondary,rightSecondary,tail;
        private Feather leftAlula,rightAlula;
        private BirdRigDriver rig;
        private Quaternion leftManusFrame,rightManusFrame,leftUpperFrame,rightUpperFrame,leftForeFrame,rightForeFrame;
        private AvianArticulationSettings settings;
        public AvianPoseState State { get; private set; }
        public void Configure(BirdRigDriver driver,AvianArticulationSettings tuning,BirdMorphology morphology=null)
        {
            rig=driver;settings=tuning;
            rig.ShoulderSweepLimitDeg=settings.ShoulderSweepDeg;
            rig.ShoulderPronationGain=settings.ShoulderPronationGain;rig.MaxShoulderPronationDeg=settings.PronationDeg;
            Quaternion Frame(Transform a,Transform b)=>Quaternion.LookRotation(a.InverseTransformDirection(b.position-a.position),a.InverseTransformDirection(transform.up));
            leftUpperFrame=Frame(rig.leftUpper,rig.leftForearm);rightUpperFrame=Frame(rig.rightUpper,rig.rightForearm);
            leftForeFrame=Frame(rig.leftForearm,rig.leftHand);rightForeFrame=Frame(rig.rightForearm,rig.rightHand);
            leftManusFrame=Quaternion.LookRotation(rig.leftHand.InverseTransformDirection(rig.leftTip.position-rig.leftHand.position),rig.leftHand.InverseTransformDirection(transform.up));
            rightManusFrame=Quaternion.LookRotation(rig.rightHand.InverseTransformDirection(rig.rightTip.position-rig.rightHand.position),rig.rightHand.InverseTransformDirection(transform.up));
            var root=rig.leftUpper; while(root.parent!=null && root.name!="Root")root=root.parent;
            var bones=root.GetComponentsInChildren<Transform>();
            Feather Capture(Transform b) => new Feather { Bone=b,Rest=b.localRotation,
                FanAxis=b.InverseTransformDirection(transform.up),PitchAxis=b.InverseTransformDirection(transform.right) };
            Feather[] Group(string prefix) => bones.Where(b=>b.name.StartsWith(prefix,StringComparison.Ordinal))
                .OrderBy(b=>b.name,StringComparer.Ordinal).Select(Capture).ToArray();
            leftPrimary=Group("LeftPrimary");rightPrimary=Group("RightPrimary");
            leftSecondary=Group("LeftSecondary");rightSecondary=Group("RightSecondary");tail=Group("Rectrix");
            leftAlula=Capture(bones.Single(b=>b.name=="LeftAlula"));rightAlula=Capture(bones.Single(b=>b.name=="RightAlula"));
            int primaryCount=morphology?.PrimariesPerWing??10, secondaryCount=morphology?.SecondariesPerWing??9, tailCount=morphology?.Rectrices??12;
            if(leftPrimary.Length!=primaryCount || rightPrimary.Length!=primaryCount || leftSecondary.Length!=secondaryCount || rightSecondary.Length!=secondaryCount || tail.Length!=tailCount)
                throw new InvalidOperationException("Avian feather hierarchy/count mismatch");
        }
        public static float CoupledFanAngle(int index,int count,float spread,float maximum,float exponent=1.35f)
        {
            // Increasing leverage along the fan, smooth near the folded state.
            float t=(index+1f)/count;
            return maximum*Mathf.Pow(t,exponent)*Mathf.SmoothStep(0f,1f,Mathf.Clamp01(spread));
        }
        public static float AlulaDeployment(float speed,float alpha,float brake,float tuck)
            => (1-Mathf.Clamp01(tuck))*Mathf.Clamp01(Mathf.InverseLerp(8f,3f,speed))
                *Mathf.Max(Mathf.InverseLerp(10f,24f,alpha),Mathf.Clamp01(brake));
        public static float TailSpread(float brake,float speed,float tuck)
            => (1-Mathf.Clamp01(tuck))*Mathf.Clamp01(.12f+.7f*brake+.18f*Mathf.InverseLerp(9f,3f,speed));
        public void Present(FlightInputFrame input,BirdTrackingCalibration calibration,BirdFlightController controller,float groundBlend,float dt)
        {
            if(rig==null)return;
            var c=controller;var heading=input.BodyTracked?input.BodyOrientation:calibration.Heading;
            float Fold(WingInput wing,bool left)
            {
                if(!wing.Tracked)return groundBlend;
                var neutral=left?calibration.LeftNeutral:calibration.RightNeutral;
                float reach=(Quaternion.Inverse(heading)*(wing.Position-(input.BodyTracked?input.HeadPosition:calibration.HeadOrigin))).magnitude;
                float rest=(Quaternion.Inverse(calibration.Heading)*(neutral-calibration.HeadOrigin)).magnitude;
                return Mathf.Max(groundBlend,Mathf.Max(Mathf.Clamp01((1-reach/Mathf.Max(.2f,rest))/Mathf.Max(.1f,settings.FoldReachFraction)),input.Tuck));
            }
            float lf=Fold(input.LeftWing,true),rf=Fold(input.RightWing,false);
            float speed=(c.State.Velocity-c.WindVelocity).magnitude;
            float brake=Mathf.Max(input.Flare,c.LandingApproach);
            State=new AvianPoseState { LeftFold=lf,RightFold=rf,LeftFan=1-lf,RightFan=1-rf,
                Alula=AlulaDeployment(speed,c.AngleOfAttackDeg,brake,input.Tuck)*(1-groundBlend),
                TailSpread=Mathf.Lerp(TailSpread(brake,speed,input.Tuck),.08f,groundBlend),
                TailPitch=Mathf.Clamp(brake*settings.TailBrakePitchDeg-c.HeadPitchInput*5f,-10f,30f),
                TailYaw=Mathf.Clamp(input.Bank*settings.TailYawDeg+c.PhysicalYawOffsetDeg*.1f,-settings.TailYawDeg,settings.TailYawDeg) };
            // Core IK supplies elevation and sweep from actual XYZ hand displacement. This
            // additional manus hinge couples reduced reach to feather folding. It never scales bones.
            if(lf>0 || rf>0)
            {
                void FoldArm(Transform upper,Transform fore,bool left,float amount)
                {
                    float sign=left?-1:1;
                    var upperGoal=Quaternion.LookRotation(transform.TransformDirection(new Vector3(sign*.12f,.03f,-1)),transform.up)*Quaternion.Inverse(left?leftUpperFrame:rightUpperFrame);
                    var foreGoal=Quaternion.LookRotation(transform.TransformDirection(new Vector3(-sign*.12f,.03f,1)),transform.up)*Quaternion.Inverse(left?leftForeFrame:rightForeFrame);
                    float blend=Mathf.SmoothStep(0,1,amount);
                    upper.rotation=Quaternion.Slerp(upper.rotation,upperGoal,blend);
                    fore.rotation=Quaternion.Slerp(fore.rotation,foreGoal,blend);
                }
                FoldArm(rig.leftUpper,rig.leftForearm,true,lf);FoldArm(rig.rightUpper,rig.rightForearm,false,rf);
                var leftGoal=Quaternion.LookRotation(transform.TransformDirection(new Vector3(-.12f,.06f,-1)),transform.up)*Quaternion.Inverse(leftManusFrame);
                var rightGoal=Quaternion.LookRotation(transform.TransformDirection(new Vector3(.12f,.06f,-1)),transform.up)*Quaternion.Inverse(rightManusFrame);
                rig.leftHand.rotation=Quaternion.Slerp(rig.leftHand.rotation,Quaternion.RotateTowards(rig.leftHand.rotation,leftGoal,settings.ManusFoldDeg),Mathf.SmoothStep(0,1,lf));
                rig.rightHand.rotation=Quaternion.Slerp(rig.rightHand.rotation,Quaternion.RotateTowards(rig.rightHand.rotation,rightGoal,settings.ManusFoldDeg),Mathf.SmoothStep(0,1,rf));
            }
            Fan(leftPrimary,State.LeftFan,settings.PrimaryFanDeg,1);
            Fan(rightPrimary,State.RightFan,settings.PrimaryFanDeg,-1);
            Fan(leftSecondary,State.LeftFan,settings.SecondaryFanDeg,1);
            Fan(rightSecondary,State.RightFan,settings.SecondaryFanDeg,-1);
            Rotate(leftAlula,0,State.Alula*settings.AlulaDeg);
            Rotate(rightAlula,0,State.Alula*settings.AlulaDeg);
            for(int i=0;i<tail.Length;i++)
            {
                float center=(tail.Length-1)*.5f;float across=(i-center)/Mathf.Max(1,center);
                Rotate(tail[i],across*State.TailSpread*settings.TailFanDeg+State.TailYaw,State.TailPitch);
            }
        }
        private void Fan(Feather[] feathers,float spread,float max,float sign)
        {
            for(int i=0;i<feathers.Length;i++)
                Rotate(feathers[i],sign*(CoupledFanAngle(i,feathers.Length,spread,max,settings.FanExponent)-CoupledFanAngle(i,feathers.Length,1,max,settings.FanExponent)),0);
        }
        private static void Rotate(Feather feather,float yaw,float pitch)
            => feather.Bone.localRotation=feather.Rest*Quaternion.AngleAxis(yaw,feather.FanAxis)*Quaternion.AngleAxis(pitch,feather.PitchAxis);
    }
}
