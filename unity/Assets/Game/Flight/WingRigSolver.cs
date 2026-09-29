using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Flight
{
    // Pure analytic two-link solve. A consistent body-space pole prevents elbow flipping.
    public static class WingRigSolver
    {
        public static Vector3 Solve(Vector3 shoulder, Vector3 target, Vector3 pole,
            float upperLength, float lowerLength, out Vector3 reachableTarget)
        {
            var delta = target - shoulder;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upperLength - lowerLength) + .005f,
                upperLength + lowerLength - .005f);
            var direction = delta.sqrMagnitude > 1e-8f ? delta.normalized : Vector3.right;
            reachableTarget = shoulder + direction * distance;
            var bend = Vector3.ProjectOnPlane(pole, direction);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.up, direction);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.forward, direction);
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            return shoulder + direction * along + bend.normalized * height;
        }
    }

    [System.Serializable]
    public sealed class BirdTrackingCalibration
    {
        public Vector3 HeadOrigin;
        public Quaternion Heading = Quaternion.identity;
        public Quaternion NeutralLookPitch = Quaternion.identity;
        public Quaternion BodyHeading { get; private set; } = Quaternion.identity;
        public Vector3 LeftNeutral = FlightInputFrame.Neutral.LeftWing.Position;
        public Vector3 RightNeutral = FlightInputFrame.Neutral.RightWing.Position;
        public Quaternion LeftRotation = Quaternion.identity, RightRotation = Quaternion.identity;
        public float HumanSpanMeters = 1.2f;
        public float BirdHalfSpanMeters = .56f;
        public float MinimumCalibrationSpanMeters = .7f;
        public float MaximumCalibrationSpanMeters = 2.2f;
        public bool CompactHands;
        // Hands calibrate where headset cameras still see them: controller flights rested
        // about 0.54 m out, and tracking held up to 0.6 m while hands stayed just forward.
        public const float CompactReachMin = .25f, CompactReachMax = .65f;
        private float MinimumSpan => CompactHands ? 2f * CompactReachMin : MinimumCalibrationSpanMeters;
        public bool Captured;
        public bool HeadCaptured;
        public float MotionScale => BirdHalfSpanMeters * 2f / Mathf.Max(.35f, HumanSpanMeters);
        // Raised and moved aft after headset feedback so the wing roots enter peripheral vision.
        public static Vector3 EyeAnchor => new Vector3(0f, .50f, -.42f);

        // Hands calibrate on a steady natural glide pose at the end of a countdown. The
        // player's own pose may sit outside camera view, so an inferred hand is accepted.
        public bool CaptureNaturalHands(FlightInputFrame frame) =>
            frame.HeadTracked && HandCalibrationFlow.HandVisible(frame.LeftWing) && HandCalibrationFlow.HandVisible(frame.RightWing)
            && HandCalibrationFlow.NaturalPose(frame, Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f), out _, out _)
            && Capture(frame, true);

        public bool Capture(FlightInputFrame frame, bool allowInferred = false)
        {
            float span = Vector3.Distance(frame.LeftWing.Position, frame.RightWing.Position);
            if (!frame.HeadTracked || !frame.LeftWing.Tracked || !frame.RightWing.Tracked
                || !allowInferred && (!MeasuredPose(frame.LeftWing) || !MeasuredPose(frame.RightWing))
                || span < MinimumSpan || span > MaximumCalibrationSpanMeters)
                return false;
            HeadOrigin = frame.HeadPosition;
            Heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            BodyHeading = Heading;
            var forward = frame.HeadOrientation * Vector3.forward;
            NeutralLookPitch = Quaternion.Euler(-Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg, 0f, 0f);
            HeadCaptured = true;
            LeftNeutral = frame.LeftWing.Position;
            RightNeutral = frame.RightWing.Position;
            LeftRotation = frame.LeftWing.Orientation;
            RightRotation = frame.RightWing.Orientation;
            HumanSpanMeters = span;
            Captured = true;
            return true;
        }

        public bool IsComfortableGlidePose(FlightInputFrame frame)
        {
            float span = Vector3.Distance(frame.LeftWing.Position, frame.RightWing.Position);
            if (!frame.HeadTracked || !frame.LeftWing.Tracked || !frame.RightWing.Tracked
                || !MeasuredPose(frame.LeftWing) || !MeasuredPose(frame.RightWing)
                || span < MinimumSpan || span > MaximumCalibrationSpanMeters)
                return false;
            var heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            var left = Quaternion.Inverse(heading) * (frame.LeftWing.Position - frame.HeadPosition);
            var right = Quaternion.Inverse(heading) * (frame.RightWing.Position - frame.HeadPosition);
            bool oppositeSides = left.x < -.25f && right.x > .25f;
            bool level = Mathf.Abs(left.y - right.y) < .18f && left.y > -.8f && left.y < .2f
                && right.y > -.8f && right.y < .2f;
            bool nearShoulderPlane = Mathf.Abs(left.z - right.z) < .25f
                && Mathf.Abs((left.z + right.z) * .5f) < .55f;
            bool compactReach = !CompactHands || (left.x <= -CompactReachMin && left.x >= -CompactReachMax
                && right.x >= CompactReachMin && right.x <= CompactReachMax);
            return oppositeSides && level && nearShoulderPlane && compactReach;
        }

        private static bool MeasuredPose(WingInput wing) => !wing.MotionEstimated
            && (wing.Source == HandPoseSource.Unknown || wing.Source == HandPoseSource.DirectHigh);

        // Same body-heading frame as IsComfortableGlidePose, for per-hand coaching.
        private static Vector3 HandOffset(FlightInputFrame frame, bool leftHand)
        {
            var heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            return Quaternion.Inverse(heading) * ((leftHand ? frame.LeftWing : frame.RightWing).Position - frame.HeadPosition);
        }

        public bool HandReachOk(FlightInputFrame frame, bool leftHand)
        {
            float outward = HandOffset(frame, leftHand).x * (leftHand ? -1f : 1f);
            return CompactHands ? outward >= CompactReachMin && outward <= CompactReachMax : outward > .25f;
        }

        // The first unmet pose condition as a short correction; null when accepted or
        // when only tracking/span limits remain.
        public string PoseHint(FlightInputFrame frame)
        {
            if (IsComfortableGlidePose(frame)) return null;
            var left = HandOffset(frame, true); var right = HandOffset(frame, false);
            float inner = CompactHands ? CompactReachMin : .25f;
            if (-left.x < inner || right.x < inner) return "SPREAD YOUR HANDS A LITTLE WIDER";
            if (CompactHands && (-left.x > CompactReachMax || right.x > CompactReachMax)) return "BRING YOUR HANDS A LITTLE CLOSER";
            if (Mathf.Abs(left.y - right.y) >= .18f) return "LEVEL YOUR HANDS";
            if (left.y >= .2f || right.y >= .2f) return "LOWER YOUR HANDS A LITTLE";
            if (left.y <= -.8f || right.y <= -.8f) return "RAISE YOUR HANDS A LITTLE";
            if (Mathf.Abs(left.z - right.z) >= .25f) return "LINE UP BOTH HANDS";
            float forward = (left.z + right.z) * .5f;
            if (forward >= .55f) return "BRING YOUR HANDS TOWARD YOUR BODY";
            if (forward <= -.55f) return "BRING YOUR HANDS A LITTLE FORWARD";
            return null;
        }

        public bool CaptureComfortableGlide(FlightInputFrame frame) =>
            IsComfortableGlidePose(frame) && Capture(frame);

        public bool CaptureHead(FlightInputFrame frame)
        {
            if (!frame.HeadTracked) return false;
            HeadOrigin = frame.HeadPosition;
            Heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            BodyHeading = Heading;
            var forward = frame.HeadOrientation * Vector3.forward;
            NeutralLookPitch = Quaternion.Euler(-Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg, 0f, 0f);
            HeadCaptured = true;
            return true;
        }

        public void BeginPlatformRecenter()
        {
            Captured = false;
            HeadCaptured = false;
        }

        public void ConfigureBirdHalfSpan(float halfSpanMeters)
        {
            BirdHalfSpanMeters = Mathf.Max(.1f, halfSpanMeters);
        }

        public Vector3 WingTarget(WingInput wing, bool left, Vector3 currentHeadPosition, Quaternion currentBodyOrientation)
        {
            var neutral = left ? LeftNeutral : RightNeutral;
            var currentHeading = Quaternion.Euler(0f, currentBodyOrientation.eulerAngles.y, 0f);
            var neutralLocal = Quaternion.Inverse(Heading) * (neutral - HeadOrigin);
            var currentLocal = Quaternion.Inverse(currentHeading) * (wing.Position - currentHeadPosition);
            return new Vector3(left ? -BirdHalfSpanMeters : BirdHalfSpanMeters, .04f, .005f)
                + (currentLocal - neutralLocal) * MotionScale;
        }
        public Vector3 WingTarget(WingInput wing, bool left) =>
            WingTarget(wing, left, HeadOrigin, Heading);
        public void UpdateBody(FlightInputFrame frame)
        {
            if (frame.BodyTracked) BodyHeading = frame.BodyOrientation;
        }
        public Quaternion WingRotation(WingInput wing, bool left) => WingRotation(wing, left, Heading);
        public Quaternion WingRotation(WingInput wing, bool left, Quaternion bodyHeading) => Quaternion.Inverse(bodyHeading)
            * wing.Orientation * Quaternion.Inverse(left ? LeftRotation : RightRotation) * Heading;
        // Head translation stays at physical scale; arm-span scaling applies only to wings.
        public Vector3 HeadOffset(Vector3 position) => Quaternion.Inverse(BodyHeading) * (position - HeadOrigin);
    }
}
