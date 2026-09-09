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
        public Quaternion BodyHeading { get; private set; } = Quaternion.identity;
        public Vector3 LeftNeutral = FlightInputFrame.Neutral.LeftWing.Position;
        public Vector3 RightNeutral = FlightInputFrame.Neutral.RightWing.Position;
        public Quaternion LeftRotation = Quaternion.identity, RightRotation = Quaternion.identity;
        public float HumanSpanMeters = 1.2f;
        public float BirdHalfSpanMeters = .56f;
        public float MinimumCalibrationSpanMeters = .7f;
        public float MaximumCalibrationSpanMeters = 2.2f;
        public bool Captured;
        public bool HeadCaptured;
        public float MotionScale => BirdHalfSpanMeters * 2f / Mathf.Max(.35f, HumanSpanMeters);
        // Raised and moved aft after headset feedback so the wing roots enter peripheral vision.
        public static Vector3 EyeAnchor => new Vector3(0f, .50f, -.42f);

        public bool Capture(FlightInputFrame frame)
        {
            float span = Vector3.Distance(frame.LeftWing.Position, frame.RightWing.Position);
            if (!frame.HeadTracked || !frame.LeftWing.Tracked || !frame.RightWing.Tracked
                || span < MinimumCalibrationSpanMeters || span > MaximumCalibrationSpanMeters)
                return false;
            HeadOrigin = frame.HeadPosition;
            Heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            BodyHeading = Heading;
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
                || span < MinimumCalibrationSpanMeters || span > MaximumCalibrationSpanMeters)
                return false;
            var heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            var left = Quaternion.Inverse(heading) * (frame.LeftWing.Position - frame.HeadPosition);
            var right = Quaternion.Inverse(heading) * (frame.RightWing.Position - frame.HeadPosition);
            bool oppositeSides = left.x < -.25f && right.x > .25f;
            bool level = Mathf.Abs(left.y - right.y) < .18f && left.y > -.8f && left.y < .2f
                && right.y > -.8f && right.y < .2f;
            bool nearShoulderPlane = Mathf.Abs(left.z - right.z) < .25f
                && Mathf.Abs((left.z + right.z) * .5f) < .55f;
            return oppositeSides && level && nearShoulderPlane;
        }

        public bool CaptureComfortableGlide(FlightInputFrame frame) =>
            IsComfortableGlidePose(frame) && Capture(frame);

        public bool CaptureHead(FlightInputFrame frame)
        {
            if (!frame.HeadTracked) return false;
            HeadOrigin = frame.HeadPosition;
            Heading = frame.BodyTracked ? frame.BodyOrientation : Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
            BodyHeading = Heading;
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
