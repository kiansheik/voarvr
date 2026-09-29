using UnityEngine;

namespace VoarVR.Input
{
    public enum HandCalibrationStep { Pose, LookLeft, LookRight, Start, Return, Countdown, Done }

    // Hands-free calibration. The player never aims their head or brings a hand in to pinch
    // while holding the pose that becomes neutral: a steady natural glide pose runs a 3-2-1
    // countdown and the final frame is captured. The first run also asks the player to look
    // left and right while holding the pose, which moves each hand out of camera view while
    // it is physically still and measures how far the inferred pose sits from the real hand.
    public sealed class HandCalibrationFlow
    {
        public const float PoseHoldSeconds = 1f, LookYawDegrees = 30f, LookHoldSeconds = .4f;
        public const float CountdownSeconds = 3f, BreakGraceSeconds = .25f, SteadySpeed = .35f;
        public const int MinimumBiasSamples = 20;
        public const float MaxBias = .6f;

        public HandCalibrationStep Step { get; private set; } = HandCalibrationStep.Done;
        public bool HeadReady { get; private set; }
        public bool LeftReady { get; private set; }
        public bool RightReady { get; private set; }
        public bool PoseReady => HeadReady && LeftReady && RightReady && poseSane && steady;
        public bool PoseSane => poseSane;
        public bool Steady => steady;
        public float CountdownRemaining => Mathf.Max(0f, CountdownSeconds - countdown);
        public bool HasContext { get; private set; }
        public bool LeftBiasKnown { get; private set; }
        public bool RightBiasKnown { get; private set; }
        // Real (camera) minus inferred hand position, in the reference yaw frame.
        public Vector3 LeftInferredBias { get; private set; }
        public Vector3 RightInferredBias { get; private set; }

        private float held, countdown, broken, referenceYaw;
        private bool poseSane, steady, havePrevious;
        private Vector3 previousLeft, previousRight;
        private Accumulator leftDirect, leftInferred, rightDirect, rightInferred;

        private struct Accumulator
        {
            public Vector3 Sum; public int Count;
            public void Add(Vector3 v) { Sum += v; Count++; }
            public Vector3 Mean => Count > 0 ? Sum / Count : Vector3.zero;
        }

        // A full run teaches the context; a redo or recenter goes straight to the countdown.
        public void Begin(bool full)
        {
            Step = full || !HasContext ? HandCalibrationStep.Pose : HandCalibrationStep.Return;
            held = countdown = broken = 0f; havePrevious = false;
            if (Step == HandCalibrationStep.Pose)
                leftDirect = leftInferred = rightDirect = rightInferred = default;
        }

        public void PressStart()
        {
            if (Step != HandCalibrationStep.Start) return;
            Step = HandCalibrationStep.Return; held = countdown = broken = 0f;
        }

        public void Cancel() { Step = HandCalibrationStep.Done; }

        // Pose in the head-yaw frame: natural and wide, camera-seen or inferred.
        public static bool NaturalPose(FlightInputFrame frame, Quaternion yaw, out Vector3 left, out Vector3 right)
        {
            var inverse = Quaternion.Inverse(yaw);
            left = inverse * (frame.LeftWing.Position - frame.HeadPosition);
            right = inverse * (frame.RightWing.Position - frame.HeadPosition);
            float span = Vector3.Distance(frame.LeftWing.Position, frame.RightWing.Position);
            return left.x < -.18f && right.x > .18f && left.x > -1f && right.x < 1f
                && left.y > -.9f && left.y < .4f && right.y > -.9f && right.y < .4f
                && Mathf.Abs(left.y - right.y) < .3f
                && left.z > -.6f && left.z < .7f && right.z > -.6f && right.z < .7f
                && span >= .5f && span <= 2.2f;
        }

        public static bool HandVisible(WingInput wing) => wing.Tracked
            && (wing.Source == HandPoseSource.DirectHigh || wing.Source == HandPoseSource.DirectLow
                || wing.Source == HandPoseSource.Inferred || wing.Source == HandPoseSource.Unknown);

        // `frame` is the continuity-filtered pose used for flight; `device` is the native
        // pose with its source, used only to learn the inferred-pose bias.
        // Returns true on the frame the countdown completes.
        public bool Sample(FlightInputFrame frame, FlightInputFrame device, float dt)
        {
            if (Step == HandCalibrationStep.Done) return false;
            dt = Mathf.Max(0f, dt);
            HeadReady = frame.HeadTracked;
            LeftReady = HandVisible(frame.LeftWing);
            RightReady = HandVisible(frame.RightWing);
            float headYaw = HeadReady ? frame.HeadOrientation.eulerAngles.y : referenceYaw;
            var poseYaw = Quaternion.Euler(0f, Step == HandCalibrationStep.LookLeft || Step == HandCalibrationStep.LookRight
                ? referenceYaw : headYaw, 0f);
            Vector3 left = default, right = default;
            poseSane = HeadReady && LeftReady && RightReady && NaturalPose(frame, poseYaw, out left, out right);
            if (poseSane && havePrevious && dt > 0f)
                steady = (left - previousLeft).magnitude / dt < SteadySpeed && (right - previousRight).magnitude / dt < SteadySpeed;
            else steady = false;
            if (poseSane) { previousLeft = left; previousRight = right; havePrevious = true; } else havePrevious = false;

            switch (Step)
            {
                case HandCalibrationStep.Pose:
                    held = PoseReady ? held + dt : 0f;
                    if (held >= PoseHoldSeconds) { referenceYaw = headYaw; Step = HandCalibrationStep.LookLeft; held = 0f; }
                    break;
                case HandCalibrationStep.LookLeft:
                case HandCalibrationStep.LookRight:
                    Learn(device);
                    float relative = Mathf.DeltaAngle(referenceYaw, headYaw);
                    bool looking = HeadReady && (Step == HandCalibrationStep.LookLeft ? relative <= -LookYawDegrees : relative >= LookYawDegrees);
                    held = looking ? held + dt : 0f;
                    if (held >= LookHoldSeconds)
                    {
                        held = 0f;
                        if (Step == HandCalibrationStep.LookLeft) Step = HandCalibrationStep.LookRight;
                        else { FinishContext(); Step = HandCalibrationStep.Start; }
                    }
                    break;
                case HandCalibrationStep.Return:
                    if (PoseReady) { Step = HandCalibrationStep.Countdown; countdown = broken = 0f; }
                    break;
                case HandCalibrationStep.Countdown:
                    // A brief tracking blip does not restart the count; leaving the pose does.
                    broken = PoseReady ? 0f : broken + dt;
                    if (broken > BreakGraceSeconds) { Step = HandCalibrationStep.Return; countdown = 0f; break; }
                    if (PoseReady) countdown += dt;
                    if (countdown >= CountdownSeconds) { Step = HandCalibrationStep.Done; return true; }
                    break;
            }
            return false;
        }

        private void Learn(FlightInputFrame device)
        {
            if (!device.HeadTracked) return;
            var inverse = Quaternion.Inverse(Quaternion.Euler(0f, referenceYaw, 0f));
            Add(device.LeftWing, inverse * (device.LeftWing.Position - device.HeadPosition), ref leftDirect, ref leftInferred);
            Add(device.RightWing, inverse * (device.RightWing.Position - device.HeadPosition), ref rightDirect, ref rightInferred);
        }

        private static void Add(WingInput wing, Vector3 relative, ref Accumulator direct, ref Accumulator inferred)
        {
            if (!wing.Tracked) return;
            if (wing.Source == HandPoseSource.DirectHigh) direct.Add(relative);
            else if (wing.Source == HandPoseSource.Inferred) inferred.Add(relative);
        }

        private void FinishContext()
        {
            HasContext = true;
            LeftBiasKnown = Bias(leftDirect, leftInferred, out var l); LeftInferredBias = l;
            RightBiasKnown = Bias(rightDirect, rightInferred, out var r); RightInferredBias = r;
        }

        private static bool Bias(Accumulator direct, Accumulator inferred, out Vector3 bias)
        {
            bias = Vector3.zero;
            if (direct.Count < MinimumBiasSamples || inferred.Count < MinimumBiasSamples) return false;
            bias = Vector3.ClampMagnitude(direct.Mean - inferred.Mean, MaxBias);
            return true;
        }
    }
}
