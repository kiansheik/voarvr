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

    public interface IWindField
    {
        Vector3 Sample(Vector3 worldPosition, float simulationTime);
        string ModeName { get; }
    }

    // Arcade-realistic embodied flight tuning. One field per tunable so behavior can be
    // iterated without touching the model, and so a future per-species profile has a home.
    [Serializable]
    public sealed class BirdFlightProfile
    {
        public static BirdFlightProfile Default => Duck();
        public static BirdFlightProfile Duck() => new BirdFlightProfile();

        // Pose-deviation gate: below these, a provider's wing pose counts as "not moved".
        public float PoseEpsDeg = 1f;
        public float PoseEpsMeters = 0.02f;

        // Roll (bank).
        public float RollHeightWeight = 0.6f;
        public float RollOrientationWeight = 0.4f;
        public float MaxRollInputDeg = 35f;
        public float RollDeadzoneDeg = 3f;
        public float MaxRollDeg = 50f;

        // Head pitch (climb/descend).
        public float MaxHeadPitchDeg = 45f;
        public float HeadPitchDeadzoneDeg = 8f;
        public float MaxPitchDeg = 35f;

        // Tuck / flare (arm span vs. trigger, additive).
        public float MaxTuckSpanRatio = 0.5f;
        public float MaxFlareSpanRatio = 0.3f;
        public float TriggerTuckWeight = 1f;
        public float TriggerFlareWeight = 1f;
        public float TuckPitchBiasDeg = 15f;
        public float FlarePitchBiasDeg = 20f;

        // Flap.
        public float MaxStrokeSpeed = 3f;
        // Duck gameplay assumptions, SI units; not measured biological constants.
        public float MassKg = 1.1f;
        public float Gravity = 9.81f;
        public float AirDensity = 1.225f;
        public float WingAreaM2 = 0.28f;
        public float BodyDragAreaM2 = 0.018f;
        public float WingDragCoefficient = 0.07f;
        public float InducedDragCoefficient = 0.12f;
        public float LiftSlopePerRadian = 4.2f;
        public float WingIncidenceDeg = 8f;
        public float StallAngleDeg = 24f;
        public float StallSpeedMps = 4f;
        public float TuckedAreaFraction = 0.12f;
        public float FlareDragCoefficient = 0.9f;
        public float StrokeForcePerSpeedSquared = 9f; // N / (m/s)^2, active wing work
        public float StrokeForwardRatio;
        public float InitialSpeedMps = 8f;
        public float AttitudeResponseSeconds = 0.3f;
        public float MaxPhysicalYawRateDeg = 240f;
        public float MaxIntegrationStep = 1f / 120f;

        // Perching.
        public float PerchCaptureRadius = 1.5f;
        public float PerchMaxCaptureSpeed = 8f;
        public float FlarePerchCaptureRadius = 2.4f;
        public float FlarePerchMaxCaptureSpeed = 10f;
        public float PerchSettleTime = 0.25f;
        public float PerchLandOffsetY = 0.05f;
        public float TakeoffFlapThreshold = 1.2f;
        public float TakeoffForwardSpeed = 6f;
        public float TakeoffLiftSpeed = 2.5f;
        public float TakeoffClearance = 0.15f;
        public float PerchRecaptureCooldown = 0.5f;
    }

    // Explicit point-mass force integration; presentation never feeds transforms back.
    public sealed class BirdFlightController
    {
        private readonly IFlightInput input;
        private readonly Vector3 spawn;
        private readonly IReadOnlyList<PerchInfo> perches;
        private readonly BirdFlightProfile profile;
        private readonly float? groundHeight;
        private readonly IWindField wind;
        private bool paused;
        private Vector3 pausedVelocity;
        public Vector3 LiftForce { get; private set; }
        public Vector3 DragForce { get; private set; }
        public Vector3 StrokeForce { get; private set; }
        public float AngleOfAttackDeg { get; private set; }
        public float MechanicalEnergy => .5f * profile.MassKg * State.Velocity.sqrMagnitude + profile.MassKg * profile.Gravity * State.Position.y;
        private FlightPhase phase;
        private float pitchDeg, rollDeg, yawDeg;
        private WingCalibration calibration;
        private float perchCooldown;
        private PerchInfo perchTarget;
        private Vector3 landedPos;
        private float landedYaw;
        private Vector3 perchVel;
        private float perchPitchVel, perchRollVel, perchYawVel;
        private float simulationTime;
        private float lastTrackedBodyYaw;
        private bool hasTrackedBodyYaw;
        public float PhysicalYawOffsetDeg { get; private set; }
        public float SimulationTime => simulationTime;
        public float NearestPerchDistance { get; private set; } = float.PositiveInfinity;
        public Vector3 NearestPerchPosition { get; private set; }
        public Vector3 WindVelocity { get; private set; }

        public BirdState State { get; private set; }
        public FlightInputFrame LastInput { get; private set; }
        public string InputMode => input.Mode;

        private struct WingCalibration
        {
            public Vector3 LeftPos, RightPos;
            public Quaternion LeftRot, RightRot;
            public Quaternion Heading;
            public float WingSpanXZ;

            // Matches FlightInputFrame.Neutral exactly: providers that never move wing
            // pose (gamepad/synthetic) always compare as "unchanged" against this default.
            public static WingCalibration FromNeutral()
            {
                var neutral = FlightInputFrame.Neutral;
                return Capture(neutral.LeftWing, neutral.RightWing, Quaternion.identity);
            }

            public static WingCalibration Capture(WingInput left, WingInput right, Quaternion headOrientation) => new WingCalibration
            {
                LeftPos = left.Position,
                RightPos = right.Position,
                LeftRot = left.Orientation,
                RightRot = right.Orientation,
                WingSpanXZ = HorizontalDistance(left.Position, right.Position),
                Heading = Quaternion.Euler(0f, headOrientation.eulerAngles.y, 0f)
            };
        }

        public BirdFlightController(IFlightInput input, Vector3 spawn,
            IReadOnlyList<PerchInfo> perches = null, BirdFlightProfile profile = null, float? groundHeight = null,
            IWindField wind = null)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.spawn = spawn;
            this.perches = perches ?? Array.Empty<PerchInfo>();
            this.profile = profile ?? BirdFlightProfile.Default;
            this.groundHeight = groundHeight;
            this.wind = wind;
            calibration = WingCalibration.FromNeutral();
            Reset();
        }

        // Calibration changes the input neutral only; it never changes position or velocity.
        public void Calibrate(FlightInputFrame frame)
        {
            var local = NormalizeBodyFrame(frame);
            if (frame.LeftWing.Tracked && frame.RightWing.Tracked)
                calibration = WingCalibration.Capture(local.LeftWing, local.RightWing,
                    frame.BodyTracked ? Quaternion.identity : frame.HeadOrientation);
            if (frame.BodyTracked)
            {
                lastTrackedBodyYaw = frame.BodyOrientation.eulerAngles.y;
                hasTrackedBodyYaw = true;
                PhysicalYawOffsetDeg = 0f;
            }
        }

        public static FlightInputFrame NormalizeBodyFrame(FlightInputFrame frame)
        {
            if (!frame.BodyTracked) return frame;
            var inverse = Quaternion.Inverse(frame.BodyOrientation);
            WingInput Local(WingInput wing)
            {
                wing.Position = inverse * (wing.Position - frame.HeadPosition);
                wing.Orientation = inverse * wing.Orientation;
                wing.Velocity = inverse * wing.Velocity;
                return wing;
            }
            frame.LeftWing = Local(frame.LeftWing);
            frame.RightWing = Local(frame.RightWing);
            return frame;
        }

        public void Reset()
        {
            paused = false;
            phase = FlightPhase.Gliding;
            pitchDeg = rollDeg = yawDeg = 0f;
            perchCooldown = 0f;
            simulationTime = 0f;
            PhysicalYawOffsetDeg = 0f;
            hasTrackedBodyYaw = false;
            LastInput = FlightInputFrame.Neutral;
            State = new BirdState { Position = spawn, Velocity = Vector3.forward * profile.InitialSpeedMps, Rotation = Quaternion.identity, Phase = FlightPhase.Gliding };
        }

        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            LastInput = NormalizeBodyFrame(input.Sample(deltaTime));

            if (LastInput.ResetPressed)
            {
                var resetFrame = LastInput;
                Reset();
                LastInput = resetFrame;
                return;
            }
            if (LastInput.PausePressed)
            {
                paused = !paused;
                if (paused) pausedVelocity = State.Velocity;
                else { var resumed = State; resumed.Velocity = pausedVelocity; State = resumed; }
            }
            if (paused)
            {
                phase = FlightPhase.Paused;
                State = new BirdState { Position = State.Position, Rotation = State.Rotation, Velocity = Vector3.zero, Phase = phase };
                return;
            }
            if (perchCooldown > 0f) perchCooldown -= deltaTime;
            simulationTime += deltaTime;

            // --- Signals -------------------------------------------------------------
            bool wingsTracked = LastInput.LeftWing.Tracked && LastInput.RightWing.Tracked;
            bool poseDeviates = wingsTracked && (
                Quaternion.Angle(calibration.LeftRot, LastInput.LeftWing.Orientation) > profile.PoseEpsDeg ||
                Quaternion.Angle(calibration.RightRot, LastInput.RightWing.Orientation) > profile.PoseEpsDeg ||
                Vector3.Distance(calibration.LeftPos, LastInput.LeftWing.Position) > profile.PoseEpsMeters ||
                Vector3.Distance(calibration.RightPos, LastInput.RightWing.Position) > profile.PoseEpsMeters);

            float rollSignal = ComputeRollSignal(poseDeviates);
            float headPitchSignal = ComputeHeadPitchSignal();
            float wingSpanRatio = wingsTracked && calibration.WingSpanXZ > 0.01f
                ? HorizontalDistance(LastInput.LeftWing.Position, LastInput.RightWing.Position) / calibration.WingSpanXZ
                : 1f;
            float tuckSignal = Mathf.Clamp01(Mathf.Clamp01((1f - wingSpanRatio) / profile.MaxTuckSpanRatio)
                + Mathf.Clamp01(LastInput.Tuck) * profile.TriggerTuckWeight);
            float flareSignal = Mathf.Clamp01(Mathf.Clamp01((wingSpanRatio - 1f) / profile.MaxFlareSpanRatio)
                + Mathf.Clamp01(LastInput.Flare) * profile.TriggerFlareWeight);
            float flapPower = ComputeFlapPower();

            // Integrate forces in bounded substeps. Input is sampled once per external step.
            // Positive bank means right; Unity positive Z roll tilts lift left, hence minus.
            float targetRollDeg = -rollSignal * profile.MaxRollDeg;
            float wingPitch = wingsTracked ? .5f * (WingPitchDeg(LastInput.LeftWing.Orientation, calibration.LeftRot)
                + WingPitchDeg(LastInput.RightWing.Orientation, calibration.RightRot)) : 0f;
            float targetPitchDeg = Mathf.Clamp(-headPitchSignal * profile.MaxPitchDeg
                + tuckSignal * profile.TuckPitchBiasDeg - flareSignal * profile.FlarePitchBiasDeg
                + wingPitch * .3f, -profile.MaxPitchDeg, profile.MaxPitchDeg);
            var velocity = State.Velocity;
            var position = State.Position;
            var rotation = State.Rotation;
            if (LastInput.BodyTracked)
            {
                float currentBodyYaw = LastInput.BodyOrientation.eulerAngles.y;
                if (hasTrackedBodyYaw)
                {
                    float physicalYawDelta = Mathf.Clamp(Mathf.DeltaAngle(lastTrackedBodyYaw, currentBodyYaw),
                        -profile.MaxPhysicalYawRateDeg * deltaTime, profile.MaxPhysicalYawRateDeg * deltaTime);
                    if (Mathf.Abs(physicalYawDelta) > .001f)
                    {
                        lastTrackedBodyYaw += physicalYawDelta;
                        PhysicalYawOffsetDeg += physicalYawDelta;
                        yawDeg += physicalYawDelta;
                        velocity = Quaternion.Euler(0f, physicalYawDelta, 0f) * velocity;
                    }
                }
                if (!hasTrackedBodyYaw) lastTrackedBodyYaw = currentBodyYaw;
                hasTrackedBodyYaw = true;
            }
            else hasTrackedBodyYaw = false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / profile.MaxIntegrationStep));
            float dt = deltaTime / steps;
            for (int step = 0; step < steps; step++)
            {
                float response = 1f - Mathf.Exp(-dt / profile.AttitudeResponseSeconds);
                rollDeg = Mathf.LerpAngle(rollDeg, targetRollDeg, response);
                pitchDeg = Mathf.LerpAngle(pitchDeg, targetPitchDeg, response);
                rotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);
                var forward = rotation * Vector3.forward;
                var up = rotation * Vector3.up;
                WindVelocity = wind?.Sample(position, simulationTime) ?? Vector3.zero;
                var airVelocity = velocity - WindVelocity;
                float speed = airVelocity.magnitude;
                var airDirection = speed > .001f ? airVelocity / speed : forward;
                // Signed incidence relative to incoming flow, including each controller's twist.
                float pathAngle = Mathf.Atan2(Vector3.Dot(airDirection, up), Vector3.Dot(airDirection, forward)) * Mathf.Rad2Deg;
                AngleOfAttackDeg = profile.WingIncidenceDeg - pathAngle - wingPitch * .5f + flareSignal * 10f;
                float alpha = Mathf.Clamp(AngleOfAttackDeg, -85f, 85f);
                float stall = Mathf.Clamp01((Mathf.Abs(alpha) - profile.StallAngleDeg) / 25f);
                float cl = profile.LiftSlopePerRadian * alpha * Mathf.Deg2Rad;
                cl = Mathf.Clamp(cl, -1.5f, 1.5f) * Mathf.Lerp(1f, .12f, stall)
                    * Mathf.Clamp01(speed / profile.StallSpeedMps);
                float area = profile.WingAreaM2 * Mathf.Lerp(1f, profile.TuckedAreaFraction, tuckSignal);
                float q = .5f * profile.AirDensity * speed * speed;
                var liftDirection = Vector3.ProjectOnPlane(up, airDirection).normalized;
                LiftForce = liftDirection * (q * area * cl);
                float dragArea = profile.BodyDragAreaM2 + area * (profile.WingDragCoefficient
                    + profile.InducedDragCoefficient * cl * cl + flareSignal * profile.FlareDragCoefficient + stall * .4f);
                DragForce = -airDirection * (q * dragArea);
                // Each controller supplies a pressure direction as well as stroke speed.
                // Sum the two wing reactions; edge-on strokes naturally do little work.
                StrokeForce = ComputeStrokeForce(rotation);
                var acceleration = Vector3.down * profile.Gravity + (LiftForce + DragForce + StrokeForce) / profile.MassKg;
                velocity += acceleration * dt;
                position += velocity * dt;
                // Yaw follows the curved velocity produced by banked lift. No commanded yaw velocity.
                if (velocity.x * velocity.x + velocity.z * velocity.z > .25f)
                    yawDeg = Mathf.LerpAngle(yawDeg, Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg, response);
            }
            rotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);

            // --- Perching ---------------------------------------------------------------
            if (phase != FlightPhase.Perched && phase != FlightPhase.Perching && perchCooldown <= 0f)
            {
                var candidate = FindEligiblePerch(position, velocity, flareSignal);
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

            // Prototype ground contact is a plane, not a general collision engine.
            if (groundHeight.HasValue && position.y < groundHeight.Value && velocity.y <= 0f)
            {
                landedPos = new Vector3(position.x, groundHeight.Value, position.z);
                landedYaw = yawDeg;
                position = landedPos; velocity = Vector3.zero; pitchDeg = rollDeg = 0f;
                rotation = Quaternion.Euler(0f, yawDeg, 0f); phase = FlightPhase.Perched;
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

        private float ComputeFlapPower()
        {
            return .5f * (Stroke(LastInput.LeftWing, calibration.LeftRot) + Stroke(LastInput.RightWing, calibration.RightRot));
        }

        private float Stroke(WingInput wing, Quaternion neutral)
        {
            if (!wing.Tracked) return 0f;
            var relative = Quaternion.Inverse(calibration.Heading) * wing.Orientation
                * Quaternion.Inverse(neutral) * calibration.Heading;
            var normal = relative * Vector3.up;
            var velocity = Quaternion.Inverse(calibration.Heading) * wing.Velocity;
            // A down/back stroke against the wing surface does work; edge-on motion does little.
            return Mathf.Clamp(Vector3.Dot(-velocity, normal), 0f, profile.MaxStrokeSpeed);
        }

        private Vector3 ComputeStrokeForce(Quaternion bodyRotation)
        {
            return WingStrokeForce(LastInput.LeftWing, calibration.LeftRot, bodyRotation)
                + WingStrokeForce(LastInput.RightWing, calibration.RightRot, bodyRotation);
        }

        private Vector3 WingStrokeForce(WingInput wing, Quaternion neutral, Quaternion bodyRotation)
        {
            if (!wing.Tracked) return Vector3.zero;
            var relative = Quaternion.Inverse(calibration.Heading) * wing.Orientation
                * Quaternion.Inverse(neutral) * calibration.Heading;
            var normalBody = relative * Vector3.up;
            var velocityBody = Quaternion.Inverse(calibration.Heading) * wing.Velocity;
            float speed = Mathf.Clamp(Vector3.Dot(-velocityBody, normalBody), 0f, profile.MaxStrokeSpeed);
            // The authored large wing sweeps backward during a human downstroke. A species
            // can convert that work to forward thrust without demanding a tilted wrist.
            // No motion (or an upstroke) still produces no active force.
            var reaction = normalBody + Vector3.forward * (profile.StrokeForwardRatio * Mathf.Max(0f, normalBody.y));
            return bodyRotation * reaction * (speed * speed * profile.StrokeForcePerSpeedSquared * .5f);
        }

        private PerchInfo? FindEligiblePerch(Vector3 pos, Vector3 velocity, float flare)
        {
            float maxSpeed = flare > .15f ? profile.FlarePerchMaxCaptureSpeed : profile.PerchMaxCaptureSpeed;
            float radius = flare > .15f ? profile.FlarePerchCaptureRadius : profile.PerchCaptureRadius;
            NearestPerchDistance = float.PositiveInfinity;
            if (perches.Count == 0) return null;
            PerchInfo? best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < perches.Count; i++)
            {
                var perch = perches[i];
                var top = perch.Position + Vector3.up * perch.TopOffset;
                float dist = Vector3.Distance(pos, top);
                if (dist < NearestPerchDistance)
                {
                    NearestPerchDistance = dist;
                    NearestPerchPosition = top;
                }
                if (velocity.magnitude > maxSpeed || dist > radius) continue;
                if (Vector3.Dot(velocity, top - pos) <= 0f) continue;
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
