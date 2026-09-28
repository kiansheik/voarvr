using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Flight
{
    // The FBX rest axes are discovered once; no dependency on importer bone-axis conventions.
    public sealed class BirdRigDriver : MonoBehaviour
    {
        public Transform leftUpper, leftForearm, leftHand, leftTip;
        public Transform rightUpper, rightForearm, rightHand, rightTip;
        public Renderer face;
        public Renderer[] firstPersonHidden;
        public Transform presentationRoot;
        // Rest-pose glide target distance from shoulder, meters; set per species (see
        // BirdCharacterDefinition.RestArmSpan) so a longer- or shorter-armed rig neither
        // overreaches nor folds at rest. Duck default preserves the original tuning.
        public float restArmSpan = 0.56f;
        // Optional joint limits configured by an articulation capability; zero leaves
        // legacy mapping untouched. Shoulder long-axis rotation preserves link length.
        public float ShoulderSweepLimitDeg;
        public float ShoulderPronationGain;
        public float MaxShoulderPronationDeg = 45f;
        // Articulated avians own their finer trigger pose in AvianWingPresentation.
        // Legacy and membrane rigs use the same gross target change through core IK.
        public bool TriggerGrossPose { get; set; } = true;

        public void SetFirstPersonVisibility(bool firstPerson)
        {
            if (firstPersonHidden == null) return;
            foreach (var renderer in firstPersonHidden)
                if (renderer != null) renderer.enabled = !firstPerson;
        }

        public void SetPresentationVisible(bool visible)
        {
            if(presentationRoot!=null && presentationRoot.gameObject.activeSelf!=visible)
                presentationRoot.gameObject.SetActive(visible);
        }
        private Wing left, right;
        public Vector3 LeftTarget { get; private set; }
        public Vector3 RightTarget { get; private set; }
        public float LeftReachError { get; private set; }
        public float RightReachError { get; private set; }
        private Vector3 impactNormalLocal=Vector3.up;
        private float impactSide,impactStrength,impactRemaining,impactDuration;
        public int ImpactRecoilSequence { get; private set; }

        private sealed class Wing
        {
            public Transform Upper, Lower, Hand, Tip;
            public Quaternion UpperRest, LowerRest, HandRest;
            public Vector3 UpperAxis, LowerAxis;
            public float UpperLength, LowerLength;
            public Vector3 LocalTarget;
            public Quaternion Twist = Quaternion.identity;
            public Wing(Transform a, Transform b, Transform c, Transform d, float restArmSpan)
            {
                Upper=a; Lower=b; Hand=c; Tip=d;
                UpperRest=a.localRotation; LowerRest=b.localRotation; HandRest=c.localRotation;
                UpperAxis=a.InverseTransformDirection(b.position-a.position).normalized;
                LowerAxis=b.InverseTransformDirection(c.position-b.position).normalized;
                UpperLength=Vector3.Distance(a.position,b.position); LowerLength=Vector3.Distance(b.position,c.position);
                LocalTarget = new Vector3(a.name.StartsWith("Left") ? -restArmSpan : restArmSpan, .04f, .005f);
            }
        }
        private void Awake() => Rebuild();

        // Called by the spawner after bone/restArmSpan fields are (re)assigned, since
        // AddComponent runs Awake synchronously and can fire before those fields are set.
        public void Configure() => Rebuild();

        private void Rebuild()
        {
            if (leftUpper != null) left = new Wing(leftUpper,leftForearm,leftHand,leftTip,restArmSpan);
            if (rightUpper != null) right = new Wing(rightUpper,rightForearm,rightHand,rightTip,restArmSpan);
        }
        public void Present(FlightInputFrame frame, BirdTrackingCalibration calibration, Quaternion heading, float dt)
        {
            if (left == null || right == null) return;
            LeftReachError=Pose(left,frame.LeftWing,true,calibration,heading,frame.BodyTracked ? frame.HeadPosition : calibration.HeadOrigin,
                frame.BodyTracked ? frame.BodyOrientation : calibration.Heading,frame.Tuck,frame.Flare,dt);
            RightReachError=Pose(right,frame.RightWing,false,calibration,heading,frame.BodyTracked ? frame.HeadPosition : calibration.HeadOrigin,
                frame.BodyTracked ? frame.BodyOrientation : calibration.Heading,frame.Tuck,frame.Flare,dt);
            LeftTarget = transform.position + heading * left.LocalTarget;
            RightTarget = transform.position + heading * right.LocalTarget;
            PresentImpact(dt);
        }

        public void AddCollisionImpulse(Vector3 worldNormal,float impactSpeed,float side)
        {
            float strength=Mathf.InverseLerp(.8f,14f,impactSpeed);
            if(strength<=0f)return;
            impactNormalLocal=transform.InverseTransformDirection(
                worldNormal.sqrMagnitude>.01f?worldNormal.normalized:Vector3.up);
            impactSide=Mathf.Clamp(side,-1f,1f);
            impactStrength=Mathf.Max(impactStrength,strength);
            impactDuration=impactRemaining=Mathf.Lerp(.14f,.24f,impactStrength);
            ImpactRecoilSequence++;
        }

        private void PresentImpact(float dt)
        {
            if(presentationRoot==null)return;
            if(impactRemaining<=0f)
            {
                presentationRoot.localPosition=Vector3.zero;
                presentationRoot.localRotation=Quaternion.identity;
                return;
            }
            impactRemaining=Mathf.Max(0f,impactRemaining-Mathf.Max(0f,dt));
            float age=1f-impactRemaining/impactDuration;
            float wave=Mathf.Sin(age*Mathf.PI*2.5f)*(1f-age);
            presentationRoot.localPosition=impactNormalLocal*Mathf.Lerp(.015f,.09f,impactStrength)*wave;
            presentationRoot.localRotation=Quaternion.Euler(
                -Mathf.Lerp(.4f,2.2f,impactStrength)*wave,
                impactSide*Mathf.Lerp(.4f,2.8f,impactStrength)*wave,
                -impactSide*Mathf.Lerp(.7f,4f,impactStrength)*wave);
        }
        private float Pose(Wing wing, WingInput input, bool isLeft, BirdTrackingCalibration calibration,
            Quaternion heading, Vector3 headPosition, Quaternion headOrientation, float tuck, float flare, float dt)
        {
            // Targets live in the same yaw-only world basis as the HMD, while shoulders follow body attitude.
            var rest = new Vector3(isLeft ? -restArmSpan : restArmSpan,.04f,.005f);
            var desiredLocal = input.Tracked
                ? calibration.WingTarget(input,isLeft,headPosition,headOrientation) : rest;
            if(TriggerGrossPose)
                desiredLocal=LegacyControlTarget(desiredLocal,restArmSpan,isLeft,tuck,flare,restArmSpan>1f);
            if(ShoulderSweepLimitDeg>0)
                desiredLocal.z=Mathf.Clamp(desiredLocal.z,-Mathf.Sin(ShoulderSweepLimitDeg*Mathf.Deg2Rad)*restArmSpan,
                    Mathf.Sin(ShoulderSweepLimitDeg*Mathf.Deg2Rad)*restArmSpan);
            var twist = input.Tracked ? calibration.WingRotation(input,isLeft,headOrientation) : Quaternion.identity;
            float blend = 1f-Mathf.Exp(-dt/(input.Tracked ? .035f : .25f));
            // Smooth only the tracked displacement. Root travel and yaw are applied after
            // smoothing so locomotion cannot consume reach or leave the wing behind.
            wing.LocalTarget = Vector3.Lerp(wing.LocalTarget, desiredLocal, blend);
            var target = transform.position + heading * wing.LocalTarget;
            wing.Twist=Quaternion.Slerp(wing.Twist,twist,blend);
            wing.Upper.localRotation=wing.UpperRest; wing.Lower.localRotation=wing.LowerRest; wing.Hand.localRotation=wing.HandRest;
            var elbow=WingRigSolver.Solve(wing.Upper.position,target,transform.forward * -.8f + transform.up * -.3f,
                wing.UpperLength,wing.LowerLength,out var reachable);
            wing.Upper.rotation=Quaternion.FromToRotation(wing.Upper.TransformDirection(wing.UpperAxis),elbow-wing.Upper.position)*wing.Upper.rotation;
            if(ShoulderPronationGain>0)
            {
                float angle=Mathf.Clamp(Mathf.DeltaAngle(0,wing.Twist.eulerAngles.x)*ShoulderPronationGain,
                    -MaxShoulderPronationDeg,MaxShoulderPronationDeg)*(isLeft?-1:1);
                wing.Upper.rotation=Quaternion.AngleAxis(angle,(elbow-wing.Upper.position).normalized)*wing.Upper.rotation;
            }
            wing.Lower.rotation=Quaternion.FromToRotation(wing.Lower.TransformDirection(wing.LowerAxis),reachable-wing.Lower.position)*wing.Lower.rotation;
            // Twist all three rotational axes at the hand; clamp relative angular reach anatomically.
            var bounded=Quaternion.RotateTowards(Quaternion.identity,wing.Twist,85f);
            wing.Hand.rotation=heading*bounded*Quaternion.Inverse(heading)*wing.Hand.rotation;
            return Vector3.Distance(reachable,target);
        }

        public static Vector3 LegacyControlTarget(Vector3 tracked,float halfSpan,bool left,float tuck,float flare,bool membrane)
        {
            float sign=left?-1f:1f;
            tuck=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(tuck));
            flare=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(flare))*(1f-tuck);
            float broad=membrane?1f:.75f;
            var flareTarget=new Vector3(sign*halfSpan*.98f,halfSpan*.2f,halfSpan*.18f*broad);
            var tuckTarget=new Vector3(sign*halfSpan*(membrane ? .28f : .34f),-halfSpan*.04f,
                -halfSpan*(membrane ? .55f : .46f));
            return Vector3.Lerp(Vector3.Lerp(tracked,flareTarget,flare),tuckTarget,tuck);
        }
    }
}
