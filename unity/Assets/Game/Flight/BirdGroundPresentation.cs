using System.Linq;
using UnityEngine;

namespace VoarVR.Flight
{
    // Procedural settling, folded wings and distance-driven footsteps for every foot joint.
    // The imported flying rest pose and tracked wing articulation are restored in the air.
    public sealed class BirdGroundPresentation : MonoBehaviour
    {
        private BirdRigDriver rig;
        private Transform root;
        private Vector3 rootRest;
        private Quaternion rootRotation;
        private Transform[] feet;
        private Vector3[] footRest;
        private Quaternion[] footRotation;
        private float blend, stride;
        private float radius;
        private Quaternion leftHandFrame,rightHandFrame,leftUpperFrame,rightUpperFrame,leftLowerFrame,rightLowerFrame;
        public float GroundBlend=>blend;
        public int LegCount=>feet?.Length??0;
        public void Configure(BirdRigDriver wings,float collisionRadius)
        {
            rig=wings;radius=collisionRadius;
            leftHandFrame=HandFrame(rig.leftHand,rig.leftTip);rightHandFrame=HandFrame(rig.rightHand,rig.rightTip);
            leftUpperFrame=HandFrame(rig.leftUpper,rig.leftForearm);rightUpperFrame=HandFrame(rig.rightUpper,rig.rightForearm);
            leftLowerFrame=HandFrame(rig.leftForearm,rig.leftHand);rightLowerFrame=HandFrame(rig.rightForearm,rig.rightHand);
            // The scene rig is destroyed at end of frame when selecting a species.
            // Follow the newly wired wing, never select that retiring duplicate root.
            root=wings.leftUpper;
            while(root!=null && root.name!="Root")root=root.parent;
            if(root==null)return;
            var bones=root.GetComponentsInChildren<Transform>();
            rootRest=root.localPosition;rootRotation=root.localRotation;
            feet=bones.Where(t=>t.name.EndsWith("Foot")).OrderBy(t=>t.name).ToArray();
            footRest=new Vector3[feet.Length];footRotation=new Quaternion[feet.Length];
            for(int i=0;i<feet.Length;i++){footRest[i]=feet[i].localPosition;footRotation[i]=feet[i].localRotation;}
        }
        public void RestoreBase()
        {
            if(root==null)return;
            root.localPosition=rootRest;root.localRotation=rootRotation;
            for(int i=0;i<feet.Length;i++){feet[i].localPosition=footRest[i];feet[i].localRotation=footRotation[i];}
        }
        public void Present(BirdFlightController c,float dt)
        {
            if(root==null)return;
            bool grounded=c.State.Phase==FlightPhase.Perched;
            blend=Mathf.MoveTowards(blend,grounded?1:0,dt*(grounded?4:7));
            float speed=new Vector2(c.GroundVelocity.x,c.GroundVelocity.z).magnitude;
            if(grounded)stride+=speed*dt/Mathf.Max(.12f,radius*.8f);
            float walk=grounded?Mathf.Clamp01(speed/.7f):0;
            float bob=Mathf.Abs(Mathf.Sin(stride*Mathf.PI))*radius*.08f*walk;
            root.localPosition=rootRest+root.parent.InverseTransformVector(transform.up*((-radius*.18f+bob)*blend));
            root.localRotation=rootRotation;
            root.rotation=Quaternion.AngleAxis(Mathf.Sin(stride*Mathf.PI)*3f*walk*blend,transform.forward)*root.rotation;
            // Fold in the bird's heading frame; no override of the physical headset.
            Fold(rig.leftUpper,rig.leftForearm,rig.leftHand,rig.leftTip,true,blend);
            Fold(rig.rightUpper,rig.rightForearm,rig.rightHand,rig.rightTip,false,blend);
            for(int i=0;i<feet.Length;i++)
            {
                var foot=feet[i];foot.localPosition=footRest[i];foot.localRotation=footRotation[i];
                var restWorld=foot.position;
                float phase=stride*Mathf.PI+(i%2)*Mathf.PI;
                float step=Mathf.Sin(phase)*radius*.4f*walk;
                var localDirection=transform.InverseTransformDirection(c.GroundVelocity);
                var direction=new Vector3(localDirection.x,0,localDirection.z).normalized;
                var restLocal=transform.InverseTransformPoint(restWorld);
                var target=restLocal+direction*step;
                target.y=-radius+.025f+Mathf.Max(0,Mathf.Cos(phase))*radius*.22f*walk;
                foot.position=Vector3.Lerp(restWorld,transform.TransformPoint(target),blend);
            }
        }
        private Quaternion HandFrame(Transform hand,Transform tip) => Quaternion.LookRotation(
            hand.InverseTransformDirection(tip.position-hand.position),hand.InverseTransformDirection(transform.up));
        private void Fold(Transform upper,Transform lower,Transform hand,Transform tip,bool left,float amount)
        {
            if(upper==null||lower==null||hand==null||amount<=0)return;
            float side=left?-1:1;
            bool broadMembrane=rig.restArmSpan>1f;
            var target=transform.TransformDirection(new Vector3(side*.25f,.03f,-1));
            var upperGoal=broadMembrane?Quaternion.FromToRotation(lower.position-upper.position,target)*upper.rotation
                :Quaternion.LookRotation(target,transform.right*-side)*Quaternion.Inverse(left?leftUpperFrame:rightUpperFrame);
            upper.rotation=Quaternion.Slerp(upper.rotation,upperGoal,amount);
            var inward=transform.TransformDirection(new Vector3(-side*.25f,.08f,1));
            var lowerGoal=broadMembrane?Quaternion.FromToRotation(hand.position-lower.position,inward)*lower.rotation
                :Quaternion.LookRotation(inward,transform.right*side)*Quaternion.Inverse(left?leftLowerFrame:rightLowerFrame);
            lower.rotation=Quaternion.Slerp(lower.rotation,lowerGoal,amount);
            // Fold primaries back along the flank as well; retaining the airborne
            // wrist orientation here would leave feathers hanging over the feet.
            if(tip!=null)
            {
                var axis=transform.TransformDirection(broadMembrane?Vector3.back:new Vector3(side*.04f,-1,-.05f));
                var normal=transform.TransformDirection(broadMembrane?Vector3.up:Vector3.right*side);
                var targetRotation=Quaternion.LookRotation(axis,normal)*Quaternion.Inverse(left?leftHandFrame:rightHandFrame);
                hand.rotation=Quaternion.Slerp(hand.rotation,targetRotation,amount);
            }
        }
    }
}
