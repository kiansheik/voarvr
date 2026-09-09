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
        // Rest-pose glide target distance from shoulder, meters; set per species (see
        // BirdCharacterDefinition.RestArmSpan) so a longer- or shorter-armed rig neither
        // overreaches nor folds at rest. Duck default preserves the original tuning.
        public float restArmSpan = 0.56f;

        public void SetFirstPersonVisibility(bool firstPerson)
        {
            if (firstPersonHidden == null) return;
            foreach (var renderer in firstPersonHidden)
                if (renderer != null) renderer.enabled = !firstPerson;
        }
        private Wing left, right;
        public Vector3 LeftTarget { get; private set; }
        public Vector3 RightTarget { get; private set; }
        public float LeftReachError { get; private set; }
        public float RightReachError { get; private set; }

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
            LeftReachError=Pose(left,frame.LeftWing,true,calibration,heading,dt);
            RightReachError=Pose(right,frame.RightWing,false,calibration,heading,dt);
            LeftTarget = transform.position + heading * left.LocalTarget;
            RightTarget = transform.position + heading * right.LocalTarget;
        }
        private float Pose(Wing wing, WingInput input, bool isLeft, BirdTrackingCalibration calibration, Quaternion heading, float dt)
        {
            // Targets live in the same yaw-only world basis as the HMD, while shoulders follow body attitude.
            var rest = new Vector3(isLeft ? -restArmSpan : restArmSpan,.04f,.005f);
            var desiredLocal = input.Tracked ? calibration.WingTarget(input,isLeft) : rest;
            var twist = input.Tracked ? calibration.WingRotation(input,isLeft) : Quaternion.identity;
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
            wing.Lower.rotation=Quaternion.FromToRotation(wing.Lower.TransformDirection(wing.LowerAxis),reachable-wing.Lower.position)*wing.Lower.rotation;
            // Twist all three rotational axes at the hand; clamp relative angular reach anatomically.
            var bounded=Quaternion.RotateTowards(Quaternion.identity,wing.Twist,85f);
            wing.Hand.rotation=heading*bounded*Quaternion.Inverse(heading)*wing.Hand.rotation;
            return Vector3.Distance(reachable,target);
        }
    }
}
