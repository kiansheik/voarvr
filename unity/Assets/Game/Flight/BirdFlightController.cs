using System;
using System.Collections.Generic;
using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Flight
{
    public enum FlightPhase { Gliding, Flapping, Diving, Flaring, Perching, Perched, Paused }

    public struct BirdState
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public FlightPhase Phase;
        public float Speed => Velocity.magnitude;
    }

    public readonly struct PerchInfo
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        // World-space Y offset from Position to the landable top surface (perches use a
        // center pivot today, not a base pivot).
        public readonly float TopOffset;

        public PerchInfo(Vector3 position, Quaternion rotation, float topOffset = 0f)
        {
            Position = position;
            Rotation = rotation;
            TopOffset = topOffset;
        }
    }

    // Arcade-realistic embodied flight tuning. One field per tunable so behavior can be
    // iterated without touching the model, and so a future per-species profile has a home.
    public sealed class BirdFlightProfile
    {
        public static readonly BirdFlightProfile Default = new BirdFlightProfile();

        // Pose-deviation gate: below these, a provider's wing pose counts as "not moved".
        public float PoseEpsDeg = 1f;
        public float PoseEpsMeters = 0.02f;

        // Roll (bank).
        public float RollHeightWeight = 0.6f;
        public float RollOrientationWeight = 0.4f;
        public float MaxRollInputDeg = 35f;
        public float RollDeadzoneDeg = 3f;
        public float MaxRollDeg = 50f;
        public float RollRateDegPerSec = 90f;

        // Head pitch (climb/descend).
        public float MaxHeadPitchDeg = 45f;
        public float HeadPitchDeadzoneDeg = 8f;
        public float MaxPitchDeg = 35f;
        public float PitchRateDegPerSec = 60f;

        // Tuck / flare (arm span vs. trigger, additive).
        public float MaxTuckSpanRatio = 0.5f;
        public float MaxFlareSpanRatio = 0.3f;
        public float TriggerTuckWeight = 1f;
        public float TriggerFlareWeight = 1f;
        public float TuckPitchBiasDeg = 15f;
        public float FlarePitchBiasDeg = 20f;

        // Yaw.
        public float MaxDragOffsetMeters = 0.3f;
        public float BankToYawRateDegPerSec = 45f;
        public float DirectYawRateDegPerSec = 15f;

        // Flap.
        public float MaxStrokeSpeed = 3f;
        public float MinFlapQuality = 0.4f;
        public float GoodFlapPitchToleranceDeg = 60f;

        // Speed / vertical rate. Matched to the legacy kinematic model when tuck/flare/flap
        // are neutral so existing deterministic tests keep passing exactly.
        public float BaseCruiseSpeed = 3f;
        public float FlapThrustCoef = 0.5f;
        public float TuckSpeedBonus = 2f;
        public float FlareSpeedPenalty = 2f;
        public float MinSpeed = 2.5f;
        public float MaxSpeed = 14f;

        public float FlapLiftCoef = 1f;
        public float TuckSinkBonus = 2f;
        public float FlareLiftBonus = 1f;

        // Perching.
        public float PerchCaptureRadius = 1.5f;
        public float PerchMaxCaptureSpeed = 8f;
        public float PerchSettleTime = 0.25f;
        public float PerchLandOffsetY = 0.05f;
        public float TakeoffFlapThreshold = 1.2f;
        public float TakeoffForwardSpeed = 3.5f;
        public float TakeoffLiftSpeed = 2.5f;
        public float TakeoffClearance = 0.15f;
        public float PerchRecaptureCooldown = 0.5f;
    }

    // Kinematic embodied-flight model: head pitch drives climb/descend, per-hand wing pose
    // (position + orientation, tracking-local) drives roll/tuck/flare, flap quality rewards
    // correct wing orientation during the stroke. Falls back to Bank/Tuck/Flare fields when a
    // provider doesn't populate real wing pose (gamepad/synthetic), so their behavior is
    // unchanged. See docs/agent/session-handoffs for the full derivation.
    public sealed class BirdFlightController
    {
        private readonly IFlightInput input;
        private readonly Vector3 spawn;
        private readonly IReadOnlyList<PerchInfo> perches;
        private readonly BirdFlightProfile profile;
        private bool paused;
        private FlightPhase phase;
        private float pitchDeg, rollDeg, yawDeg;
        private WingCalibration calibration;
        private float perchCooldown;
        private PerchInfo perchTarget;
        private Vector3 landedPos;
        private float landedYaw;
        private Vector3 perchVel;
        private float perchPitchVel, perchRollVel, perchYawVel;

        public BirdState State { get; private set; }
        public FlightInputFrame LastInput { get; private set; }
        public string InputMode => input.Mode;

        private struct WingCalibration
        {
            public Vector3 LeftPos, RightPos;
            public Quaternion LeftRot, RightRot;
            public float WingSpanXZ;

            // Matches FlightInputFrame.Neutral exactly: providers that never move wing
            // pose (gamepad/synthetic) always compare as "unchanged" against this default.
            public static WingCalibration FromNeutral()
            {
                var neutral = FlightInputFrame.Neutral;
                return Capture(neutral.LeftWing, neutral.RightWing);
            }

            public static WingCalibration Capture(WingInput left, WingInput right) => new WingCalibration
            {
                LeftPos = left.Position,
                RightPos = right.Position,
                LeftRot = left.Orientation,
                RightRot = right.Orientation,
                WingSpanXZ = HorizontalDistance(left.Position, right.Position)
            };
        }

        public BirdFlightController(IFlightInput input, Vector3 spawn,
            IReadOnlyList<PerchInfo> perches = null, BirdFlightProfile profile = null)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.spawn = spawn;
            this.perches = perches ?? Array.Empty<PerchInfo>();
            this.profile = profile ?? BirdFlightProfile.Default;
            calibration = WingCalibration.FromNeutral();
            Reset();
        }

        public void Reset()
        {
            paused = false;
            phase = FlightPhase.Gliding;
            pitchDeg = rollDeg = yawDeg = 0f;
            perchCooldown = 0f;
            LastInput = FlightInputFrame.Neutral;
            State = new BirdState { Position = spawn, Rotation = Quaternion.identity, Phase = FlightPhase.Gliding };
        }

        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            LastInput = input.Sample(deltaTime);

            if (LastInput.ResetPressed)
            {
                if (LastInput.LeftWing.Tracked && LastInput.RightWing.Tracked)
                    calibration = WingCalibration.Capture(LastInput.LeftWing, LastInput.RightWing);
                Reset();
                return;
            }
            if (LastInput.PausePressed) paused = !paused;
            if (paused)
            {
                phase = FlightPhase.Paused;
                State = new BirdState { Position = State.Position, Rotation = State.Rotation, Velocity = Vector3.zero, Phase = phase };
                return;
            }
            if (perchCooldown > 0f) perchCooldown -= deltaTime;

            // --- Signals -------------------------------------------------------------
            bool wingsTracked = LastInput.LeftWing.Tracked && LastInput.RightWing.Tracked;
            bool poseDeviates = wingsTracked && (
                Quaternion.Angle(calibration.LeftRot, LastInput.LeftWing.Orientation) > profile.PoseEpsDeg ||
                Quaternion.Angle(calibration.RightRot, LastInput.RightWing.Orientation) > profile.PoseEpsDeg ||
                Vector3.Distance(calibration.LeftPos, LastInput.LeftWing.Position) > profile.PoseEpsMeters ||
                Vector3.Distance(calibration.RightPos, LastInput.RightWing.Position) > profile.PoseEpsMeters);

            float rollSignal = ComputeRollSignal(poseDeviates);
            float headPitchSignal = ComputeHeadPitchSignal();
            float wingSpanRatio = calibration.WingSpanXZ > 0.01f
                ? HorizontalDistance(LastInput.LeftWing.Position, LastInput.RightWing.Position) / calibration.WingSpanXZ
                : 1f;
            float tuckSignal = Mathf.Clamp01(Mathf.Clamp01((1f - wingSpanRatio) / profile.MaxTuckSpanRatio)
                + Mathf.Clamp01(LastInput.Tuck) * profile.TriggerTuckWeight);
            float flareSignal = Mathf.Clamp01(Mathf.Clamp01((wingSpanRatio - 1f) / profile.MaxFlareSpanRatio)
                + Mathf.Clamp01(LastInput.Flare) * profile.TriggerFlareWeight);
            float yawFromDragSignal = ComputeYawFromDragSignal();
            float flapPower = ComputeFlapPower();

            // --- Attitude integration --------------------------------------------------
            float targetRollDeg = rollSignal * profile.MaxRollDeg;
            float targetPitchDeg = Mathf.Clamp(
                headPitchSignal * profile.MaxPitchDeg + tuckSignal * profile.TuckPitchBiasDeg - flareSignal * profile.FlarePitchBiasDeg,
                -profile.MaxPitchDeg, profile.MaxPitchDeg);
            rollDeg = Mathf.MoveTowardsAngle(rollDeg, targetRollDeg, profile.RollRateDegPerSec * deltaTime);
            pitchDeg = Mathf.MoveTowardsAngle(pitchDeg, targetPitchDeg, profile.PitchRateDegPerSec * deltaTime);
            float bankInducedYawRate = (rollDeg / profile.MaxRollDeg) * profile.BankToYawRateDegPerSec;
            float directYawRate = yawFromDragSignal * profile.DirectYawRateDegPerSec;
            yawDeg += (bankInducedYawRate + directYawRate) * deltaTime;

            var rotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);
            var forward = rotation * Vector3.forward;

            float speed = Mathf.Clamp(
                profile.BaseCruiseSpeed + flapPower * profile.FlapThrustCoef + tuckSignal * profile.TuckSpeedBonus - flareSignal * profile.FlareSpeedPenalty,
                profile.MinSpeed, profile.MaxSpeed);
            float verticalRate = flapPower * profile.FlapLiftCoef - tuckSignal * profile.TuckSinkBonus + flareSignal * profile.FlareLiftBonus;

            var velocity = forward * speed + Vector3.up * verticalRate;
            var position = State.Position + velocity * deltaTime;

            // --- Perching ---------------------------------------------------------------
            if (phase != FlightPhase.Perched && phase != FlightPhase.Perching && perchCooldown <= 0f)
            {
                var candidate = FindEligiblePerch(position, velocity);
                if (candidate != null)
                {
                    phase = FlightPhase.Perching;
                    perchTarget = candidate.Value;
                    landedYaw = yawDeg;
                    landedPos = perchTarget.Position + Vector3.up * (perchTarget.TopOffset + profile.PerchLandOffsetY);
                    perchVel = Vector3.zero;
                    perchPitchVel = perchRollVel = perchYawVel = 0f;
                }
            }

            if (phase == FlightPhase.Perching)
            {
                position = Vector3.SmoothDamp(State.Position, landedPos, ref perchVel, profile.PerchSettleTime, Mathf.Infinity, deltaTime);
                pitchDeg = Mathf.SmoothDampAngle(pitchDeg, 0f, ref perchPitchVel, profile.PerchSettleTime, Mathf.Infinity, deltaTime);
                rollDeg = Mathf.SmoothDampAngle(rollDeg, 0f, ref perchRollVel, profile.PerchSettleTime, Mathf.Infinity, deltaTime);
                yawDeg = Mathf.SmoothDampAngle(yawDeg, landedYaw, ref perchYawVel, profile.PerchSettleTime, Mathf.Infinity, deltaTime);
                velocity = Vector3.zero;
                rotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);
                if (Vector3.Distance(position, landedPos) < 0.02f) phase = FlightPhase.Perched;
            }
            else if (phase == FlightPhase.Perched)
            {
                position = landedPos;
                velocity = Vector3.zero;
                if (flapPower > profile.TakeoffFlapThreshold)
                {
                    phase = FlightPhase.Flapping;
                    var launchForward = Quaternion.Euler(0f, yawDeg, 0f) * Vector3.forward;
                    velocity = launchForward * profile.TakeoffForwardSpeed + Vector3.up * profile.TakeoffLiftSpeed;
                    position += Vector3.up * profile.TakeoffClearance;
                    perchCooldown = profile.PerchRecaptureCooldown;
                }
            }
            else
            {
                phase = tuckSignal > 0.15f ? FlightPhase.Diving
                    : flareSignal > 0.15f ? FlightPhase.Flaring
                    : flapPower > 0.15f ? FlightPhase.Flapping
                    : FlightPhase.Gliding;
            }

            State = new BirdState { Position = position, Velocity = velocity, Rotation = rotation, Phase = phase };
        }

        private float ComputeRollSignal(bool poseDeviates)
        {
            float rollDegRaw;
            if (poseDeviates)
            {
                float posDeltaLeftY = LastInput.LeftWing.Position.y - calibration.LeftPos.y;
                float posDeltaRightY = LastInput.RightWing.Position.y - calibration.RightPos.y;
                float armHalfSpan = Mathf.Max(0.01f, calibration.WingSpanXZ * 0.5f);
                float rollFromHeightDeg = Mathf.Atan2(posDeltaLeftY - posDeltaRightY, armHalfSpan) * Mathf.Rad2Deg;
                float rollFromOrientationDeg = 0.5f * (
                    WingRollDeg(LastInput.RightWing.Orientation, calibration.RightRot) -
                    WingRollDeg(LastInput.LeftWing.Orientation, calibration.LeftRot));
                rollDegRaw = profile.RollHeightWeight * rollFromHeightDeg + profile.RollOrientationWeight * rollFromOrientationDeg;
            }
            else
            {
                rollDegRaw = Mathf.Clamp(LastInput.Bank, -1f, 1f) * profile.MaxRollInputDeg;
            }
            float deadzoned = ApplyDeadzone(rollDegRaw, profile.RollDeadzoneDeg);
            return Mathf.Clamp(deadzoned / profile.MaxRollInputDeg, -1f, 1f);
        }

        private float ComputeHeadPitchSignal()
        {
            float headPitchDeg = Mathf.Asin(Mathf.Clamp(LastInput.LookDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
            float deadzoned = ApplyDeadzone(headPitchDeg, profile.HeadPitchDeadzoneDeg);
            return Mathf.Clamp(deadzoned / profile.MaxHeadPitchDeg, -1f, 1f);
        }

        private float ComputeYawFromDragSignal()
        {
            float forwardAsymmetry = (LastInput.RightWing.Position.z - calibration.RightPos.z)
                - (LastInput.LeftWing.Position.z - calibration.LeftPos.z);
            return Mathf.Clamp(forwardAsymmetry / profile.MaxDragOffsetMeters, -1f, 1f);
        }

        private float ComputeFlapPower()
        {
            float flapStrokeRaw = Mathf.Clamp(-0.5f * (LastInput.LeftWing.Velocity.y + LastInput.RightWing.Velocity.y), 0f, profile.MaxStrokeSpeed);
            float leftPitchErr = Mathf.Abs(WingPitchDeg(LastInput.LeftWing.Orientation, calibration.LeftRot));
            float rightPitchErr = Mathf.Abs(WingPitchDeg(LastInput.RightWing.Orientation, calibration.RightRot));
            float quality = 0.5f * (QualityFromPitchErr(leftPitchErr) + QualityFromPitchErr(rightPitchErr));
            return flapStrokeRaw * quality;
        }

        private float QualityFromPitchErr(float errDeg) =>
            Mathf.Lerp(profile.MinFlapQuality, 1f, Mathf.Clamp01(1f - errDeg / profile.GoodFlapPitchToleranceDeg));

        private PerchInfo? FindEligiblePerch(Vector3 pos, Vector3 velocity)
        {
            if (perches.Count == 0 || velocity.magnitude > profile.PerchMaxCaptureSpeed) return null;
            PerchInfo? best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < perches.Count; i++)
            {
                var perch = perches[i];
                float dist = Vector3.Distance(pos, perch.Position);
                if (dist > profile.PerchCaptureRadius) continue;
                if (Vector3.Dot(velocity, perch.Position - pos) <= 0f) continue;
                if (dist < bestDist) { bestDist = dist; best = perch; }
            }
            return best;
        }

        private static float ApplyDeadzone(float value, float deadzone) =>
            Mathf.Abs(value) <= deadzone ? 0f : value - Mathf.Sign(value) * deadzone;

        private static float HorizontalDistance(Vector3 a, Vector3 b) =>
            Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static Vector3 WingForward(Quaternion rot) => rot * Vector3.forward;
        private static Vector3 WingUp(Quaternion rot) => rot * Vector3.up;

        private static float WingPitchDeg(Quaternion current, Quaternion neutral)
        {
            var axis = neutral * Vector3.right;
            return Vector3.SignedAngle(WingForward(neutral), WingForward(current), axis);
        }

        private static float WingRollDeg(Quaternion current, Quaternion neutral)
        {
            var fwd = WingForward(neutral);
            var a = Vector3.ProjectOnPlane(WingUp(neutral), fwd);
            var b = Vector3.ProjectOnPlane(WingUp(current), fwd);
            return Vector3.SignedAngle(a, b, fwd);
        }
    }
}
