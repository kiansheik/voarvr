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

    public interface IWindField
    {
        Vector3 Sample(Vector3 worldPosition, float simulationTime);
        string ModeName { get; }
    }

    // Optional environment assistance; core force integration remains device independent.
    public interface IWindAssistance
    {
        bool AutomaticFeathering { get; }
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
        public float HeadPitchDeadzoneDeg = 2f;
        public float FullDownPitchDeg = 30f;
        public float DownLookDeadzoneDeg = 9f;
        public float NeutralClimbPitchDeg = 0f;
        public float ComfortableClimbPitchDeg = 12f;
        public float WalkSpeedMps = 1.25f;
        public float FullUpPitchDeg = 12f;
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
        public float WingPitchSensitivity = 1f;
        public float StallAngleDeg = 24f;
        public float AlulaStallDelayDeg; // Optional avian approximation; zero retains legacy.
        public float TailDragAreaM2;
        public float TailTurnGain;
        public float StallSpeedMps = 4f;
        public float TuckedAreaFraction = 0.12f;
        public float FlareDragCoefficient = 0.9f;
        public float StrokeForcePerSpeedSquared = 9f; // N / (m/s)^2, active wing work
        public float StrokeForwardRatio;
        public float InitialSpeedMps = 8f;
        public float AttitudeResponseSeconds = 0.3f;
        public float MaxPhysicalYawRateDeg = 240f;
        public float MaxIntegrationStep = 1f / 120f;
        public float CollisionRadius = .22f;
        public float LandingWingRaiseMeters = .18f;
        public float LandingPoseHoldSeconds = .3f;

        // Perching.
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
        private Vector3 spawn;
        private readonly IFlightEnvironment environment;
        private int landedSurface, missedSurface;
        public FlightControlMode ControlMode { get; private set; }
        public AcrobaticDynamics Acrobatic { get; } = new AcrobaticDynamics();
        public TrickDetector Tricks { get; } = new TrickDetector();
        public Vector3 ThermalGradient { get; private set; }
        public float ThermalAssist { get; private set; }
        private float circlingSeconds;
        public AcrobaticProfile AdvancedProfile => advancedProfile;
        public void ConfigureAdvanced(AcrobaticProfile value){if(value!=null)advancedProfile=value;}
        private AcrobaticProfile advancedProfile;
        public void SetControlMode(FlightControlMode mode)
        {
            if(mode==ControlMode)return;
            ControlMode=mode; Acrobatic.Reset();Tricks.Reset();
            var e=State.Rotation.eulerAngles;pitchDeg=Mathf.DeltaAngle(0,e.x);rollDeg=Mathf.DeltaAngle(0,e.z);yawDeg=e.y;
        }
        public bool MissedLanding { get; private set; }
        public int CollisionCount { get; private set; }
        public int LandingCount { get; private set; }
        public int TakeoffCount { get; private set; }
        public float EffectiveStallAngleDeg { get; private set; } = 24f;
        public bool IsStalled => Mathf.Abs(AngleOfAttackDeg)>EffectiveStallAngleDeg;
        public bool StreamingBlocked { get; set; }
        private readonly BirdFlightProfile profile;
        private readonly IWindField wind;
        private bool paused;
        private FlightPhase phaseBeforePause;
        private Vector3 pausedVelocity;
        public Vector3 LiftForce { get; private set; }
        public Vector3 DragForce { get; private set; }
        public Vector3 StrokeForce { get; private set; }
        public float SpanRatio {get;private set;}
        public float InferredTuck {get;private set;}
        public float FeatherSupport {get;private set;}
        public float AngleOfAttackDeg { get; private set; }
        public float MechanicalEnergy => .5f * profile.MassKg * State.Velocity.sqrMagnitude + profile.MassKg * profile.Gravity * State.Position.y;
        private FlightPhase phase;
        private float pitchDeg, rollDeg, yawDeg;
        private WingCalibration calibration;
        private float perchCooldown;
        private Vector3 landedPos;
        private float landedYaw;
        private float simulationTime;
        private float lastTrackedBodyYaw;
        private bool hasTrackedBodyYaw;
        public float PhysicalYawOffsetDeg { get; private set; }
        public float SimulationTime => simulationTime;
        public float LastImpactSpeed { get; private set; }
        public float NearestPerchDistance { get; private set; } = float.PositiveInfinity;
        public Vector3 NearestPerchPosition { get; private set; }
        public Vector3 WindVelocity { get; private set; }
        public float WingFeatherDeg { get; private set; }
        public float LandingApproach { get; private set; }
        public Vector3 GroundVelocity { get; private set; }
        private float neutralHeadPitch, landingPoseHold;
        private bool calibratedTrackedHead;
        public float HeadPitchInput { get; private set; }
        public float LandingBrake { get; private set; }

        public BirdState State { get; private set; }
        public FlightInputFrame LastInput { get; private set; }
        public string InputMode => input.Mode;
        public bool IsPaused => paused;
        public bool HasSupportedPerch => (paused ? phaseBeforePause : phase) == FlightPhase.Perched
            && environment != null && environment.IsSupported(landedPos, profile.CollisionRadius, landedSurface);

        // UI pause uses the same state transition as the physical X button.
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            var state = State;
            if (paused)
            {
                pausedVelocity = state.Velocity;
                phaseBeforePause = phase;
                state.Velocity = Vector3.zero;
                state.Phase = phase = FlightPhase.Paused;
            }
            else
            {
                state.Velocity = pausedVelocity;
                state.Phase = phase = phaseBeforePause;
            }
            State = state;
            Tricks.Reset();
        }

        // Explicit recovery is a supported relocation, never a fabricated landing event.
        // Callers must load destination collision and invalidate objective/catch sweeps.
        public bool TryRecoverToPerch(Vector3 expectedCenter, float heading)
        {
            if (environment == null || !Finite(expectedCenter) || !float.IsFinite(heading)) return false;
            if (!environment.Sweep(expectedCenter + Vector3.up * .75f,
                    expectedCenter - Vector3.up * 1.25f, profile.CollisionRadius, out var contact)
                || !contact.Landable || contact.Normal.y < Mathf.Cos(FlightContactSolver.MaximumSlopeDegrees * Mathf.Deg2Rad)
                || !Finite(contact.Position) || Vector3.Distance(contact.Position, expectedCenter) > 1.5f
                || !environment.IsSupported(contact.Position, profile.CollisionRadius, contact.SurfaceId)) return false;
            landedPos = contact.Position;
            landedSurface = contact.SurfaceId;
            landedYaw = yawDeg = heading;
            pitchDeg = rollDeg = 0f;
            phaseBeforePause = FlightPhase.Perched;
            phase = paused ? FlightPhase.Paused : FlightPhase.Perched;
            pausedVelocity = GroundVelocity = Vector3.zero;
            Acrobatic.Reset(); Tricks.Reset();
            LiftForce = DragForce = StrokeForce = Vector3.zero;
            LastImpactSpeed = LandingApproach = LandingBrake = landingPoseHold = 0f;
            perchCooldown = 0f;
            State = new BirdState { Position = landedPos, Velocity = Vector3.zero,
                Rotation = Quaternion.Euler(0, heading, 0), Phase = phase };
            return true;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

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
            BirdFlightProfile profile = null, float? groundHeight = null,
            IWindField wind = null, IFlightEnvironment environment = null)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.spawn = spawn;
            this.profile = profile ?? BirdFlightProfile.Default;
            this.environment = environment ?? (groundHeight.HasValue ? new PlaneFlightEnvironment(groundHeight.Value - this.profile.CollisionRadius) : null);
            this.wind = wind;
            calibration = WingCalibration.FromNeutral();
            Reset();
        }

        // Calibration changes the input neutral only; it never changes position or velocity.
        public void Calibrate(FlightInputFrame frame)
        {
            neutralHeadPitch = LookPitch(frame.LookDirection);
            calibratedTrackedHead = frame.HeadTracked;
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
            ControlMode=FlightControlMode.Beginner;Acrobatic.Reset();Tricks.Reset(true);
            advancedProfile??=AcrobaticProfile.ForMass(profile.MassKg);
            MissedLanding = false;
            LastImpactSpeed = 0f;
            paused = false;
            phase = FlightPhase.Gliding;
            pitchDeg = rollDeg = yawDeg = 0f;
            perchCooldown = 0f;
            simulationTime = 0f;
            PhysicalYawOffsetDeg = 0f;
            hasTrackedBodyYaw = false;
            LastInput = FlightInputFrame.Neutral;
            landingPoseHold = LandingBrake = LandingApproach = WingFeatherDeg = 0f;
            GroundVelocity = Vector3.zero;ThermalGradient=Vector3.zero;ThermalAssist=circlingSeconds=0;
            State = new BirdState { Position = spawn, Velocity = Vector3.forward * profile.InitialSpeedMps, Rotation = Quaternion.identity, Phase = FlightPhase.Gliding };
        }

        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            LastImpactSpeed = 0f;
            GroundVelocity = Vector3.zero;
            LastInput = NormalizeBodyFrame(input.Sample(deltaTime));

            if (LastInput.ResetPressed)
            {
                var resetFrame = LastInput;
                Reset();
                LastInput = resetFrame;
                return;
            }
            if(LastInput.ControlModePressed) SetControlMode(ControlMode==FlightControlMode.Beginner?FlightControlMode.Acrobatic:FlightControlMode.Beginner);
            if (LastInput.PausePressed)
            {
                SetPaused(!paused);
            }
            if (paused)
            {
                phase = FlightPhase.Paused;
                State = new BirdState { Position = State.Position, Rotation = State.Rotation, Velocity = Vector3.zero, Phase = phase };
                return;
            }
            if (StreamingBlocked) return;
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
            HeadPitchInput = headPitchSignal;
            float wingSpanRatio = wingsTracked && calibration.WingSpanXZ > 0.01f
                ? HorizontalDistance(LastInput.LeftWing.Position, LastInput.RightWing.Position) / calibration.WingSpanXZ
                : 1f;
            float tuckSignal = Mathf.Clamp01(Mathf.Clamp01((1f - wingSpanRatio) / profile.MaxTuckSpanRatio)
                + Mathf.Clamp01(LastInput.Tuck) * profile.TriggerTuckWeight);
            SpanRatio=wingSpanRatio;InferredTuck=tuckSignal;
            float flareSignal = Mathf.Clamp01(Mathf.Clamp01((wingSpanRatio - 1f) / profile.MaxFlareSpanRatio)
                + Mathf.Clamp01(LastInput.Flare) * profile.TriggerFlareWeight);
            float raised = wingsTracked ? Mathf.Min(LastInput.LeftWing.Position.y - calibration.LeftPos.y,
                LastInput.RightWing.Position.y - calibration.RightPos.y) : 0f;
            bool heldRaised = wingsTracked && wingSpanRatio > .85f && raised >= profile.LandingWingRaiseMeters
                && LastInput.LeftWing.Velocity.magnitude < .4f && LastInput.RightWing.Velocity.magnitude < .4f;
            // A relaxed raised recovery stroke in open air must not become a full
            // landing brake. Enter only during a nearby descending approach; keep the
            // deliberate held pose latched while it slows that same approach.
            bool landingApproach = environment != null && NearestPerchDistance < 18f && State.Velocity.y < -.2f;
            landingPoseHold = heldRaised && (landingPoseHold > 0f || landingApproach)
                ? Mathf.Min(profile.LandingPoseHoldSeconds, landingPoseHold + deltaTime) : 0f;
            LandingBrake = Mathf.Max(flareSignal, landingPoseHold >= profile.LandingPoseHoldSeconds ? 1f : 0f);
            flareSignal = LandingBrake;
            float flapPower = ComputeFlapPower();

            // Integrate forces in bounded substeps. Input is sampled once per external step.
            // Positive bank means right; Unity positive Z roll tilts lift left, hence minus.
            ThermalAssist=0;ThermalGradient=Vector3.zero;
            circlingSeconds=Mathf.Abs(rollSignal)>.12f?circlingSeconds+deltaTime:0;
            if(ControlMode==FlightControlMode.Beginner && wind is VoarVR.World.IThermalGuidance guidance && guidance.CenteringEnabled && circlingSeconds>.6f)
            {
                ThermalGradient=guidance.LiftGradient(State.Position,simulationTime);
                ThermalAssist=VoarVR.World.ThermalCentering.Bias(ThermalGradient,State.Rotation,rollSignal,wind.Sample(State.Position,simulationTime).y,tuckSignal);
            }
            float targetRollDeg = -(rollSignal+ThermalAssist) * profile.MaxRollDeg;
            float wingPitch = wingsTracked ? .5f * (WingPitchDeg(LastInput.LeftWing.Orientation, calibration.LeftRot)
                + WingPitchDeg(LastInput.RightWing.Orientation, calibration.RightRot)) * profile.WingPitchSensitivity : 0f;
            float headTarget = headPitchSignal >= 0f
                ? -Mathf.Lerp(profile.NeutralClimbPitchDeg, profile.ComfortableClimbPitchDeg, headPitchSignal)
                : -headPitchSignal * profile.MaxPitchDeg - profile.NeutralClimbPitchDeg;
            float targetPitchDeg = Mathf.Clamp(headTarget + tuckSignal * profile.TuckPitchBiasDeg
                - flareSignal * profile.FlarePitchBiasDeg, -profile.MaxPitchDeg, profile.MaxPitchDeg);
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
                        if(ControlMode==FlightControlMode.Beginner) velocity = Quaternion.Euler(0f, physicalYawDelta, 0f) * velocity;
                        else rotation=Quaternion.Euler(0f,physicalYawDelta,0f)*rotation;
                    }
                }
                if (!hasTrackedBodyYaw) lastTrackedBodyYaw = currentBodyYaw;
                hasTrackedBodyYaw = true;
            }
            else hasTrackedBodyYaw = false;
            // Stable support belongs to the environment, not a copied startup perch list.
            if (phase == FlightPhase.Perched)
            {
                if (environment != null && !environment.IsSupported(landedPos, profile.CollisionRadius, landedSurface))
                    phase = FlightPhase.Gliding;
                else if (flapPower > profile.TakeoffFlapThreshold)
                {
                    phase = FlightPhase.Flapping;
                    TakeoffCount++;
                    velocity = Quaternion.Euler(0f,yawDeg,0f) * Vector3.forward * profile.TakeoffForwardSpeed
                        + Vector3.up * profile.TakeoffLiftSpeed;
                    position = landedPos + Vector3.up * profile.TakeoffClearance;
                    perchCooldown = profile.PerchRecaptureCooldown;
                }
                else
                {
                    WalkOnSurface(deltaTime);
                    return;
                }
            }
            MissedLanding = false;
            LandingApproach = 0f;
            if(environment != null && perchCooldown <= 0 && velocity.y < .1f
                && environment.Sweep(position, position + velocity * .65f + Vector3.down * .35f,
                    profile.CollisionRadius, out var ahead)
                && ahead.Landable && ahead.Normal.y >= Mathf.Cos(FlightContactSolver.MaximumSlopeDegrees*Mathf.Deg2Rad))
            {
                LandingApproach = 1f;
                // A short automatic flare dissipates speed before actual swept contact.
                velocity.x *= Mathf.Exp(-deltaTime*2.5f); velocity.z *= Mathf.Exp(-deltaTime*2.5f);
                if(velocity.y < -1.5f)velocity.y=Mathf.Lerp(velocity.y,-1.5f,1-Mathf.Exp(-deltaTime*3f));
                targetPitchDeg = -8f;
            }

            bool contactedLanding=false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / profile.MaxIntegrationStep));
            float dt = deltaTime / steps;
            for (int step = 0; step < steps; step++)
            {
                float response = 1f - Mathf.Exp(-dt / profile.AttitudeResponseSeconds);
                if(ControlMode==FlightControlMode.Beginner)
                {
                    rollDeg = Mathf.LerpAngle(rollDeg, targetRollDeg, response);
                    pitchDeg = Mathf.LerpAngle(pitchDeg, targetPitchDeg, response);
                    rotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);
                }
                else
                {
                    float wrist=wingsTracked?.5f*(AcrobaticDynamics.CalibratedPitch(LastInput.LeftWing.Orientation,calibration.LeftRot)+AcrobaticDynamics.CalibratedPitch(LastInput.RightWing.Orientation,calibration.RightRot)):0;
                    float sweep=wingsTracked?((LastInput.LeftWing.Position.z-calibration.LeftPos.z)-(LastInput.RightWing.Position.z-calibration.RightPos.z))/.4f:0;
                    var command=wingsTracked?new Vector3(AcrobaticDynamics.SoftInput(wrist,6,32),AcrobaticDynamics.SoftInput(sweep,.12f,1)*.4f,-AcrobaticDynamics.SoftInput(rollSignal,.08f,1)):Vector3.zero;
                    rotation=Acrobatic.Step(rotation,command,(velocity-WindVelocity).magnitude,tuckSignal,dt,advancedProfile,Quaternion.Inverse(rotation)*(velocity-WindVelocity));
                }
                var forward = rotation * Vector3.forward;
                var up = rotation * Vector3.up;
                WindVelocity = wind?.Sample(position, simulationTime) ?? Vector3.zero;
                var airVelocity = velocity - WindVelocity;
                float speed = airVelocity.magnitude;
                var airDirection = speed > .001f ? airVelocity / speed : forward;
                // Signed incidence relative to incoming flow, including each controller's twist.
                float pathAngle = Mathf.Atan2(Vector3.Dot(airDirection, up), Vector3.Dot(airDirection, forward)) * Mathf.Rad2Deg;
                // Wrist articulation trims the wing; it must not reverse neutral lift
                // simply because a human rolls their hands during an ordinary stroke.
                float wristTrim = Mathf.Clamp(wingPitch * .5f, -4f, 4f);
                AngleOfAttackDeg = profile.WingIncidenceDeg - pathAngle - wristTrim + flareSignal * 10f;
                // In Assisted air, birds feather into changing airflow before a deep stall.
                // This only adjusts aerodynamic incidence: no added force or pose rewriting.
                // Explicit braking/tucking retain their original stall/dive behavior.
                // Mild relaxed arm shortening must not disable soaring protection.
                float featherSupport=ControlMode==FlightControlMode.Beginner
                    ? (1-Mathf.InverseLerp(.30f,.65f,tuckSignal))*(1-Mathf.InverseLerp(.05f,.3f,LastInput.Tuck))
                    : (tuckSignal<.1f?1:0);
                FeatherSupport=featherSupport;
                WingFeatherDeg = wind is IWindAssistance assistance && assistance.AutomaticFeathering
                    && WindVelocity.sqrMagnitude > .01f && flareSignal < .1f
                    ? Mathf.Clamp(AngleOfAttackDeg - (profile.StallAngleDeg - 4f), 0f, 45f)*featherSupport : 0f;
                AngleOfAttackDeg -= WingFeatherDeg;
                float alpha = Mathf.Clamp(AngleOfAttackDeg, -85f, 85f);
                float alula = AvianWingPresentation.AlulaDeployment(speed,alpha,Mathf.Max(flareSignal,LandingApproach),tuckSignal);
                EffectiveStallAngleDeg=profile.StallAngleDeg+(alpha>0?alula*profile.AlulaStallDelayDeg:0);
                float stall = Mathf.Clamp01((Mathf.Abs(alpha) - EffectiveStallAngleDeg) / 25f);
                float cl = profile.LiftSlopePerRadian * alpha * Mathf.Deg2Rad;
                cl = Mathf.Clamp(cl, -1.5f, 1.5f) * Mathf.Lerp(1f, .12f, stall)
                    * Mathf.Clamp01(speed / profile.StallSpeedMps);
                float area = profile.WingAreaM2 * Mathf.Lerp(1f, profile.TuckedAreaFraction, tuckSignal);
                float q = .5f * profile.AirDensity * speed * speed;
                var liftDirection = Vector3.ProjectOnPlane(up, airDirection).normalized;
                LiftForce = liftDirection * (q * area * cl);
                float dragArea = profile.BodyDragAreaM2 + area * (profile.WingDragCoefficient
                    + profile.InducedDragCoefficient * cl * cl + flareSignal * profile.FlareDragCoefficient + stall * .4f);
                float tailSpread = AvianWingPresentation.TailSpread(flareSignal,speed,tuckSignal);
                dragArea += profile.TailDragAreaM2*tailSpread*.35f;
                DragForce = -airDirection * (q * dragArea);
                // Tail authority changes heading response, not speed or free lift. The
                // aggregate point-mass model does not resolve a separate tail pitching moment.
                if(ControlMode==FlightControlMode.Beginner && profile.TailTurnGain>0f)
                    yawDeg += -rollDeg*profile.TailTurnGain*tailSpread*Mathf.Clamp01(speed/5f)*dt;
                // Each controller supplies a pressure direction as well as stroke speed.
                // Sum the two wing reactions; edge-on strokes naturally do little work.
                StrokeForce = ComputeStrokeForce(rotation);
                var acceleration = Vector3.down * profile.Gravity + (LiftForce + DragForce + StrokeForce) / profile.MassKg;
                velocity += acceleration * dt;
                if (environment != null)
                {
                    var contact = FlightContactSolver.Resolve(environment,position,velocity,dt,profile.CollisionRadius,
                        flareSignal > .5f, perchCooldown <= 0f);
                    position=contact.Position; velocity=contact.Velocity; CollisionCount+=contact.ContactCount;
                    LastImpactSpeed = Mathf.Max(LastImpactSpeed, contact.ImpactSpeed);

                    if (contact.Landed)
                    {
                        contactedLanding=true; landedPos=position; landedYaw=yawDeg; landedSurface=contact.SurfaceId;
                        pitchDeg=rollDeg=0f; LandingCount++; break;
                    }
                }
                else position += velocity * dt;
                // Yaw follows the curved velocity produced by banked lift. No commanded yaw velocity.
                if (ControlMode==FlightControlMode.Beginner && velocity.x * velocity.x + velocity.z * velocity.z > .25f)
                    yawDeg = Mathf.LerpAngle(yawDeg, Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg, response);
            }
            if(ControlMode==FlightControlMode.Beginner || contactedLanding) rotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);
            if(contactedLanding)Acrobatic.Reset();
            Tricks.Step(rotation,velocity,deltaTime,ControlMode==FlightControlMode.Acrobatic && !contactedLanding && wingsTracked);

            phase = contactedLanding ? FlightPhase.Perched : tuckSignal > .15f ? FlightPhase.Diving
                : flareSignal > .15f ? FlightPhase.Flaring : flapPower > .15f ? FlightPhase.Flapping : FlightPhase.Gliding;
            NearestPerchDistance=float.PositiveInfinity;
            if (environment != null && environment.TryFindLanding(position,24f,out var candidate))
            { NearestPerchDistance=candidate.Distance; NearestPerchPosition=candidate.Position; }
            State = new BirdState { Position = position, Velocity = velocity, Rotation = rotation, Phase = phase };
        }

        private void WalkOnSurface(float dt)
        {
            GroundVelocity=Vector3.zero;
            landedYaw=yawDeg;
            var move=Vector2.ClampMagnitude(LastInput.GroundMove,1f);
            if(move.magnitude > .15f && environment != null)
            {
                var wish=Quaternion.Euler(0,yawDeg,0)*new Vector3(move.x,0,move.y)*profile.WalkSpeedMps;
                var next=landedPos+wish*dt;
                const float step=.18f;
                // Keep collision protection while stepping over small terrain changes.
                if(environment.Sweep(landedPos+Vector3.up*step,next+Vector3.up*step,profile.CollisionRadius,out var obstacle))
                    next=new Vector3(obstacle.Position.x,next.y,obstacle.Position.z);
                if(environment.Sweep(next+Vector3.up*step,next-Vector3.up*step,profile.CollisionRadius,out var support)
                    && support.Landable && support.Normal.y >= Mathf.Cos(FlightContactSolver.MaximumSlopeDegrees*Mathf.Deg2Rad))
                {
                    GroundVelocity=(support.Position-landedPos)/dt;
                    landedPos=support.Position; landedSurface=support.SurfaceId;
                }
                else
                {
                    phase=FlightPhase.Gliding;
                    State=new BirdState {Position=next,Velocity=wish,Rotation=Quaternion.Euler(0,yawDeg,0),Phase=phase};
                    return;
                }
            }
            State=new BirdState {Position=landedPos,Velocity=GroundVelocity,Rotation=Quaternion.Euler(0,landedYaw,0),Phase=FlightPhase.Perched};
        }

        public void RebaseOrigin(Vector3 delta)
        {
            var state=State; state.Position-=delta; State=state;
            spawn-=delta; landedPos-=delta; NearestPerchPosition-=delta;
        }
        public void SetSpawn(Vector3 position) => spawn=position;

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
            if (calibratedTrackedHead && !LastInput.HeadTracked) return 0f;
            float headPitchDeg = LookPitch(LastInput.LookDirection) - neutralHeadPitch;
            float deadzone = headPitchDeg < 0f ? profile.DownLookDeadzoneDeg : profile.HeadPitchDeadzoneDeg;
            float deadzoned = ApplyDeadzone(headPitchDeg, deadzone);
            float full = headPitchDeg < 0f ? profile.FullDownPitchDeg : profile.FullUpPitchDeg;
            return Mathf.Clamp(deadzoned / Mathf.Max(1f, full - deadzone), -1f, 1f);
        }
        private static float LookPitch(Vector3 direction) => Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

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
