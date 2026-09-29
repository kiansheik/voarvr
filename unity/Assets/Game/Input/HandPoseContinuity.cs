using UnityEngine;

namespace VoarVR.Input
{
    // Vendor-free source transitions. A pose can remain visible while its motion has
    // no authority; recovery never differentiates across the missing interval.
    public sealed class HandPoseContinuity
    {
        public const float GraceSeconds = .18f;
        // Recorded Quest sessions flip source about once a second per hand; each flip costs
        // this warmup plus a stable capture interval before motion regains authority.
        public const float RecoverySeconds = .06f;
        // Inferred (Wide Motion Mode) poses landed a median 25-39 cm from the last camera
        // pose. Keep the wing continuous, then let the offset relax at walking pace.
        public const float MaxInferredOffset = .6f, InferredOffsetRelax = .15f;
        // Once calibration has measured this hand's inferred-pose bias, smooth motion within a
        // settled inferred stretch may earn part of a flap. Edges and jumps never do.
        public const float InferredStrokeAuthority = .5f, InferredSettleSeconds = .15f, InferredMaxSpeed = 4f;
        private WingInput previous;
        private Vector3 inferredOffset, inferredBias, inferredMotion;
        private bool inferredBiasKnown, haveInferredMotion;
        private double inferredTimestamp;
        private float inferredSeconds;
        private Vector3 previousMotion, lastVelocity;
        private double previousTimestamp;
        private float missingSeconds, recoverySeconds, sampleAge;
        private bool hasPose, hasMotion, measuredStream;
        private HandPoseSource source = HandPoseSource.Lost;

        public WingInput Sample(WingInput reading, Vector3 head, Quaternion heading, float dt)
        {
            dt = Mathf.Max(0, dt);
            bool available = reading.Tracked && Finite(reading.Position) && Valid(reading.Orientation);
            var nextSource = available ? reading.Source : HandPoseSource.Lost;
            bool changed = nextSource != source || hasPose && measuredStream != reading.HasUnextrapolatedPose;
            if (changed) { hasMotion = false; lastVelocity = Vector3.zero; recoverySeconds = RecoverySeconds; }
            source = nextSource;
            if (!available)
            {
                missingSeconds += dt;
                var held = previous;
                held.Tracked = hasPose && missingSeconds <= GraceSeconds;
                held.Source = HandPoseSource.Lost;
                held.HasUnextrapolatedPose = false;
                held.Velocity = Vector3.zero; held.MotionEstimated = true;
                return held;
            }
            missingSeconds = 0;
            measuredStream = reading.HasUnextrapolatedPose;
            var biasTarget = inferredBiasKnown ? heading * inferredBias : Vector3.zero;
            if (nextSource != HandPoseSource.Inferred) { inferredOffset = Vector3.zero; inferredSeconds = 0f; haveInferredMotion = false; }
            else if (changed && hasPose && previous.Tracked)
            { inferredOffset = Vector3.ClampMagnitude(previous.Position - reading.Position, MaxInferredOffset); inferredSeconds = 0f; haveInferredMotion = false; }
            else { inferredOffset = Vector3.MoveTowards(inferredOffset, biasTarget, InferredOffsetRelax * dt); inferredSeconds += dt; }
            reading.Position += inferredOffset;
            var result = reading;
            if (hasPose && (changed || recoverySeconds > 0))
            {
                result.Position = Vector3.Lerp(previous.Position, reading.Position, Mathf.Clamp01(dt / .06f));
                result.Orientation = Quaternion.Slerp(previous.Orientation, reading.Orientation, Mathf.Clamp01(dt / .06f));
            }
            recoverySeconds = Mathf.Max(0, recoverySeconds - dt);
            // Use capture-time, unextrapolated motion when available. Display prediction
            // never substitutes for a missing measured interval or a source transition.
            double timestamp = reading.HasUnextrapolatedPose ? reading.UnextrapolatedTimestamp : reading.SampleTimestamp;
            var motionPosition = reading.HasUnextrapolatedPose ? reading.UnextrapolatedPosition : reading.Position;
            var motion = Quaternion.Inverse(heading) * (motionPosition - head);
            bool direct = nextSource == HandPoseSource.DirectHigh && timestamp > 0 && Finite(motion);
            bool estimated = !direct || recoverySeconds > 0;
            sampleAge += dt;
            if (direct && (!hasMotion || timestamp > previousTimestamp))
            {
                double interval = timestamp - previousTimestamp;
                bool stableInterval = hasMotion && interval >= .004 && interval <= .15;
                var delta = motion - previousMotion;
                // Treat discontinuities as reacquisition, not a powerful flap.
                if (stableInterval && delta.magnitude > .3f)
                { recoverySeconds = RecoverySeconds; estimated = true; stableInterval = false; }
                lastVelocity = stableInterval && !estimated
                    ? heading * Vector3.ClampMagnitude(delta / (float)interval, 8f) : Vector3.zero;
                estimated |= !stableInterval;
                previousMotion = motion; previousTimestamp = timestamp; hasMotion = true; sampleAge = 0;
            }
            else if (!direct || timestamp < previousTimestamp || sampleAge > .1f)
            { hasMotion = false; lastVelocity = Vector3.zero; estimated = true; }
            result.MotionEstimated = estimated;
            result.Velocity = estimated ? Vector3.zero : lastVelocity;
            result.EstimatedStrokeAuthority = 0f;
            if (nextSource == HandPoseSource.Inferred) InferredMotion(ref result, reading, head, heading);
            hasPose = true; previous = result;
            return result;
        }

        // Same-source, bias-corrected inferred motion. The pose stays MotionEstimated, so it
        // never counts as measured effort, calibration input or a takeoff stroke.
        private void InferredMotion(ref WingInput result, WingInput reading, Vector3 head, Quaternion heading)
        {
            var motion = Quaternion.Inverse(heading) * (reading.Position - head);
            double timestamp = reading.SampleTimestamp;
            bool valid = inferredBiasKnown && timestamp > 0 && Finite(motion);
            if (valid && haveInferredMotion && timestamp > inferredTimestamp)
            {
                double interval = timestamp - inferredTimestamp;
                var delta = motion - inferredMotion;
                if (interval >= .004 && interval <= .15 && delta.magnitude <= .3f && inferredSeconds >= InferredSettleSeconds)
                {
                    result.Velocity = heading * Vector3.ClampMagnitude(delta / (float)interval, InferredMaxSpeed);
                    result.EstimatedStrokeAuthority = InferredStrokeAuthority;
                }
            }
            if (valid && (!haveInferredMotion || timestamp > inferredTimestamp))
            { inferredMotion = motion; inferredTimestamp = timestamp; haveInferredMotion = true; }
        }

        // Real minus inferred hand position in the body heading frame, from calibration.
        public void SetInferredBias(Vector3 bias) { inferredBias = Vector3.ClampMagnitude(bias, MaxInferredOffset); inferredBiasKnown = true; }

        public static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
        public static bool Valid(Quaternion q) => float.IsFinite(q.x) && float.IsFinite(q.y)
            && float.IsFinite(q.z) && float.IsFinite(q.w) && q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w > .1f;
        public void ResetMotion() { hasMotion=false;lastVelocity=Vector3.zero;recoverySeconds=RecoverySeconds; }
        public void Reset()
        {
            hasPose = hasMotion = false; source = HandPoseSource.Lost;
            previous = default; previousMotion = lastVelocity = inferredOffset = Vector3.zero;
            haveInferredMotion = false; inferredSeconds = 0f; // the calibrated bias is context and persists
            previousTimestamp = 0; missingSeconds = recoverySeconds = sampleAge = 0;
        }
    }
}
