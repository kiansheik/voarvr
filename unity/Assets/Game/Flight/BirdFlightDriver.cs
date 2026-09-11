using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using VoarVR.Input;
using VoarVR.World;
using VoarVR.UI;
using VoarVR.Gameplay;

namespace VoarVR.Flight
{
    public enum FlightInputMode { Auto, XR, Gamepad, Synthetic }
    public enum FlightViewMode { FirstPerson, ThirdPerson }

    public sealed class BirdFlightDriver : MonoBehaviour
    {
        [SerializeField] private FlightInputMode inputMode = FlightInputMode.Auto;
        [SerializeField] private SyntheticGesture gesture = SyntheticGesture.Glide;
        [SerializeField] private BirdFlightProfile profile = BirdFlightProfile.Duck();
        [SerializeField] private BirdTrackingCalibration calibration = new BirdTrackingCalibration();
        [SerializeField] private FlightViewMode viewMode = FlightViewMode.ThirdPerson;
        private BirdRigDriver rig;
        private BirdGroundPresentation groundPresentation;
        private FlightFeedback feedback;
        private AvianWingPresentation avian;
        private VoarVR.Telemetry.FlightTelemetry telemetry;
        public VoarVR.Gameplay.ExpeditionDirector Expedition {get;private set;}
        private BirdCharacterDefinition character;
        private WindField wind;
        private WorldStreamer world;
        private Vector3 originalSpawn;
        private int lastLandingCount;
        private readonly List<XRInputSubsystem> xrSubsystems = new List<XRInputSubsystem>();
        private bool platformRecenterPending;
        private float stableRecenterSeconds;
        private FlightInputFrame previousRecenterFrame;
        private bool hasRecenterFrame;
        private bool returningToSelection;
        private float coachUntil;
        private float comfortYaw;
        private bool comfortTransition;
        private FlightControlMode previousControlMode;
        private int lastTrickCount;
        private int lastTrickScore;
        private FlightMenu flightMenu;
        private JourneyPresentation journeyPresentation;
        private bool recoveryPending, recoveryFallback, resumeNeedsCalibration;
        private LogicalPosition recoveryTarget;
        private float recoveryWait;
        private bool menuToggle, menuSelect;
        private bool applicationFocused = true, applicationPaused, lastObservedPaused;
        private int menuNavigation;
        public bool RecoveryPending => recoveryPending;
        public bool ResumeNeedsCalibration => resumeNeedsCalibration;
        public BirdTrackingCalibration Calibration => calibration;
        public string CalibrationStatus { get; private set; } = "Spread arms and press A to start + calibrate";
        public string CoachStatus { get; private set; }
        public bool FlightMenuVisible => flightMenu != null && flightMenu.Visible;
        public bool ShowFlightText => !FlightMenuVisible && GetComponent<VoarVR.UI.FlightHud>()?.Visible == true;
        public Vector3 ImmersiveEyeAnchor => character!=null ? character.FirstPersonEyeAnchor*character.RigPresentationScale : new Vector3(0,.288f,.32f);
        public FlightViewMode ViewMode => viewMode;
        public float PhysicalYawOffsetDeg => Controller != null ? Controller.PhysicalYawOffsetDeg : 0f;
        public float CharacterRestHalfSpan => character != null ? character.RestArmSpan : .56f;
        public Vector3 CameraEyeAnchor => BirdTrackingCalibration.EyeAnchor
            + Vector3.up * (Mathf.Max(0f, CharacterRestHalfSpan - .56f) * .22f);
        public string WindModeName => wind != null ? wind.ModeName : "Still air";
        public Quaternion Heading => Quaternion.Euler(0f, Controller!=null && (Controller.ControlMode==FlightControlMode.Acrobatic || comfortTransition) ? comfortYaw : transform.eulerAngles.y, 0f);
        public SyntheticGesture Gesture { get => gesture; set => gesture = value; }
        public void SetSyntheticGesture(SyntheticGesture value, bool resetClock)
        {
            gesture = value;
            if (resetClock && input is SyntheticFlightInput synthetic) synthetic.ResetClock();
        }
        private IFlightInput input;
        private FlightActionGate actionGate;
        public BirdFlightController Controller { get; private set; }
        public bool UsesXR { get; private set; }

        private void Start()
        {
            viewMode = FlightViewMode.ThirdPerson;
            // Auto chooses XR on device; macOS Editor remains hardware-independent.
            UsesXR = inputMode == FlightInputMode.XR || (inputMode == FlightInputMode.Auto
                && Application.platform == RuntimePlatform.Android && !Application.isEditor);
            if (UsesXR)
                input = new XRFlightInput();
            else if (inputMode == FlightInputMode.Gamepad || (inputMode == FlightInputMode.Auto && Gamepad.current != null))
                input = new GamepadFlightInput();
            else
                input = new SyntheticFlightInput(gesture);
            character = CharacterSelection.Chosen != null ? CharacterSelection.Chosen
                : Resources.Load<BirdCharacterDefinition>($"{CharacterSelection.ResourcesFolder}/{CharacterSelection.DefaultCharacterName}");
            CharacterSelection.Chosen = null; // Consume-once: a later direct load of this scene falls back to Duck, not a stale pick.
            if (character != null)
            {
                SpawnCharacterRig(character);
                profile = character.BuildProfile();
                calibration.ConfigureBirdHalfSpan(character.RestArmSpan);
            }
            wind = FindAnyObjectByType<WindField>();
            world = FindAnyObjectByType<WorldStreamer>();
            wind?.ConfigureSpecies(profile);
            originalSpawn = transform.position;
            var environment = gameObject.AddComponent<UnityFlightEnvironment>();
            actionGate = new FlightActionGate(input);
            Controller = new BirdFlightController(actionGate, originalSpawn, profile:profile, wind:wind, environment:environment);
            rig = GetComponent<BirdRigDriver>();
            groundPresentation = gameObject.AddComponent<BirdGroundPresentation>();
            groundPresentation.Configure(rig,profile.CollisionRadius);
            feedback = gameObject.AddComponent<FlightFeedback>();
            feedback.Configure(this, wind, rig);
            if(character!=null && character.Architecture==WingArchitecture.ArticulatedAvian)
            {
                avian=gameObject.AddComponent<AvianWingPresentation>();
                avian.Configure(rig,character.Articulation,character.Morphology);
            }
            Expedition=gameObject.AddComponent<VoarVR.Gameplay.ExpeditionDirector>();
            Expedition.Configure(this,world!=null?world.Space:null);
            if(world!=null)gameObject.AddComponent<VoarVR.Gameplay.SkyForaging>().Configure(this,world.Space,wind);
            telemetry=gameObject.AddComponent<VoarVR.Telemetry.FlightTelemetry>();
            telemetry.Configure(this,rig,world,character,profile);
            if (UsesXR)
            {
                SubsystemManager.GetSubsystems(xrSubsystems);
                foreach (var subsystem in xrSubsystems) subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
            }
            // Complete essential rig/input wiring before optional landing presentation.
            var worldPresentation = FindAnyObjectByType<ProceduralFlightWorld>();
            gameObject.AddComponent<LandingGuide>().Configure(this, worldPresentation != null ? worldPresentation.LandingMaterial : null);
            gameObject.AddComponent<VoarVR.UI.FlightHud>().Configure(this, Camera.main, world);
            gameObject.AddComponent<BirdAirflowTrails>().Configure(this, worldPresentation != null ? worldPresentation.AirflowMaterial : null,
                world != null ? world.Space : null);
            flightMenu = gameObject.AddComponent<FlightMenu>();
            flightMenu.Configure(Camera.main, HandleMenuAction, MenuStatus);
            if (world != null)
            {
                journeyPresentation = gameObject.AddComponent<JourneyPresentation>();
                journeyPresentation.Configure(world.Space, Expedition);
                gameObject.AddComponent<RouteBearing>().Configure(this, journeyPresentation, Camera.main);
            }
            if (Expedition.ResumeCheckpoint != null)
            {
                resumeNeedsCalibration = UsesXR;
                CalibrationStatus = "Journey resumed: recalibrate here from the flight menu";
                BeginPerchRecovery(Expedition.ResumeCheckpoint.HasSafePerch
                    ? Expedition.ResumeCheckpoint.SafePerch : DeparturePerch(), !Expedition.ResumeCheckpoint.HasSafePerch);
            }
            if (UsesXR) Debug.Log("Duck calibration: spread both tracked arms comfortably, then press the right primary button.");
        }

        // Replaces any rig baked in by the editor Configure tools with the selected species'
        // model, then rewires BirdRigDriver by the same bone-name contract every rig exports
        // under (Left/RightUpper/Forearm/Hand/Tip, a "*Face" renderer).
        private void SpawnCharacterRig(BirdCharacterDefinition selected)
        {
            if (selected.RigModel == null)
                throw new InvalidOperationException(selected.DisplayName + " has no RigModel; run VoarVR/Configure Characters.");
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            var rigInstance = Instantiate(selected.RigModel, transform);
            rigInstance.name = selected.DisplayName + "Rig";
            rigInstance.transform.localPosition = Vector3.zero;
            rigInstance.transform.localRotation = Quaternion.identity;
            rigInstance.transform.localScale = Vector3.one * selected.RigPresentationScale;
            var renderers = rigInstance.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = Enumerable.Repeat(selected.BodyMaterial, renderer.sharedMaterials.Length).ToArray();
                if (renderer is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true; // Also solve the pose when wings leave the HMD frustum.
                    skin.localBounds = new Bounds(Vector3.zero, Vector3.one * 3f * Mathf.Max(selected.Size, 1f));
                }
            }
            var bones = rigInstance.GetComponentsInChildren<Transform>();
            var rigDriver = GetComponent<BirdRigDriver>();
            if (rigDriver == null) rigDriver = gameObject.AddComponent<BirdRigDriver>();
            rigDriver.leftUpper = bones.Single(t => t.name == "LeftUpper");
            rigDriver.leftForearm = bones.Single(t => t.name == "LeftForearm");
            rigDriver.leftHand = bones.Single(t => t.name == "LeftHand");
            rigDriver.leftTip = bones.Single(t => t.name == "LeftTip");
            rigDriver.rightUpper = bones.Single(t => t.name == "RightUpper");
            rigDriver.rightForearm = bones.Single(t => t.name == "RightForearm");
            rigDriver.rightHand = bones.Single(t => t.name == "RightHand");
            rigDriver.rightTip = bones.Single(t => t.name == "RightTip");
            rigDriver.face = renderers.Single(r => r.name.EndsWith("Face"));
            rigDriver.firstPersonHidden = renderers.Where(r => selected.FirstPersonHiddenParts.Any(suffix => r.name.EndsWith(suffix))).ToArray();
            rigDriver.restArmSpan = selected.RestArmSpan;
            rigDriver.Configure();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ReturnToCharacterSelect();
                return;
            }
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) GetComponent<VoarVR.UI.FlightHud>()?.Toggle();
            if(Keyboard.current!=null && Keyboard.current.cKey.wasPressedThisFrame) { Controller.SetControlMode(Controller.ControlMode==FlightControlMode.Beginner?FlightControlMode.Acrobatic:FlightControlMode.Beginner); ShowMode(); }
            if (Keyboard.current != null)
            {
                if (Keyboard.current.xKey.wasPressedThisFrame) Controller.SetPaused(!Controller.IsPaused);
                if (Keyboard.current.bKey.wasPressedThisFrame) ToggleView();
                menuToggle = Keyboard.current.f1Key.wasPressedThisFrame;
                menuSelect = Keyboard.current.enterKey.wasPressedThisFrame;
                menuNavigation = Keyboard.current.upArrowKey.wasPressedThisFrame ? -1
                    : Keyboard.current.downArrowKey.wasPressedThisFrame ? 1 : 0;
            }
            if (Time.deltaTime > 0f) Tick(Mathf.Min(Time.deltaTime, .05f));
        }

        // Explicit runtime step also used by scene regression tests and editor evidence capture.
        public void Tick(float deltaTime)
        {
            if (returningToSelection) return;
            ProcessPerchRecovery(deltaTime);
            if (input is SyntheticFlightInput synthetic) synthetic.Gesture = gesture;
            // Once per rendered frame so Input System poses and derivative sampling agree.
            // Tests use explicit fixed steps; presentation delta is capped after editor stalls.
            Controller.StreamingBlocked = recoveryPending || resumeNeedsCalibration || (world != null && (!world.IsReadyAt(Controller.State.Position)
                || !world.IsReadyAt(Controller.State.Position + Controller.State.Velocity * deltaTime)));
            Controller.Step(deltaTime);
            if (recoveryPending || resumeNeedsCalibration || !applicationFocused || applicationPaused) Controller.SetPaused(true);
            var forward=Controller.State.Rotation*Vector3.forward;
            if(previousControlMode==FlightControlMode.Acrobatic && Controller.ControlMode==FlightControlMode.Beginner)comfortTransition=true;
            if(Controller.LastInput.ResetPressed)comfortTransition=false;
            if(Controller.ControlMode==FlightControlMode.Beginner && !comfortTransition)comfortYaw=Controller.State.Rotation.eulerAngles.y;
            else if(Controller.ControlMode==FlightControlMode.Beginner)
            {
                comfortYaw=AdvanceComfortYaw(comfortYaw,Controller.State.Rotation.eulerAngles.y,deltaTime);
                if(Mathf.Abs(Mathf.DeltaAngle(comfortYaw,Controller.State.Rotation.eulerAngles.y))<.1f)comfortTransition=false;
            }
            else if(Mathf.Abs(forward.y)<.8f && Vector3.Dot(Controller.State.Rotation*Vector3.up,Vector3.up)>.2f)
                comfortYaw=AdvanceComfortYaw(comfortYaw,Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,deltaTime);
            previousControlMode=Controller.ControlMode;
            if (world != null && (Mathf.Abs(Controller.State.Position.x)>768f || Mathf.Abs(Controller.State.Position.z)>768f))
            {
                var p=Controller.State.Position;
                RebaseWorld(new Vector3(Mathf.Floor(p.x/128f)*128f,0f,Mathf.Floor(p.z/128f)*128f));
            }
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
            var xr = input as XRFlightInput;
            var frame = xr != null ? xr.LastRawFrame : Controller.LastInput;
            if(frame.ControlModePressed)ShowMode();
            if (Controller.Tricks.Count != lastTrickCount)
            {
                if (Controller.Tricks.Count > lastTrickCount)
                {
                    CoachStatus = Controller.Tricks.Last.ToString().ToUpperInvariant()+"  +"+Mathf.Max(0, Controller.Tricks.Score-lastTrickScore);
                    coachUntil = Time.unscaledTime + 2;
                    feedback?.TrickCue();
                }
                lastTrickCount = Controller.Tricks.Count;
                lastTrickScore = Controller.Tricks.Score;
            }
            if (frame.HudTogglePressed) GetComponent<VoarVR.UI.FlightHud>()?.Toggle();
            if (frame.CharacterSelectPressed) { ReturnToCharacterSelect(); return; }
            if (xr != null && !calibration.HeadCaptured) calibration.CaptureHead(frame);
            if (xr != null) calibration.UpdateBody(frame);
            if (frame.ViewTogglePressed)
            {
                ToggleView();
                CoachStatus = viewMode == FlightViewMode.FirstPerson ? "FIRST-PERSON VIEW" : "THIRD-PERSON VIEW";
                coachUntil = Time.unscaledTime + 2.5f;
            }
            if (frame.WindModePressed && wind != null)
            {
                wind.CycleMode();
                CoachStatus = "WIND: " + wind.ModeName.ToUpperInvariant();
                coachUntil = Time.unscaledTime + 3f;
            }
            if (frame.RecalibratePressed) RestartAndCalibrate(frame);
            else if (platformRecenterPending) TryCompletePlatformRecenter(frame, deltaTime);
            bool menuInputAvailable = applicationFocused && !applicationPaused && !platformRecenterPending
                && (!UsesXR || (frame.HeadTracked && frame.LeftWing.Tracked && frame.RightWing.Tracked));
            if (menuInputAvailable)
                flightMenu?.HandleInput(frame, Controller.IsPaused, menuToggle, menuNavigation, menuSelect);
            else { flightMenu?.Close(); flightMenu?.RequireInputRelease(); actionGate?.RequireRelease(); }
            menuToggle = menuSelect = false; menuNavigation = 0;
            if (returningToSelection) return;
            if (Controller.LandingCount != lastLandingCount)
            {
                lastLandingCount=Controller.LandingCount;
                RecordSupportedPerch();
                CoachStatus="LANDED - LEFT STICK: WALK / FLAP: FLY"; coachUntil=Time.unscaledTime+3f;
            }
            if (!platformRecenterPending && Time.unscaledTime >= coachUntil)
            {
                if (Controller.State.Phase == FlightPhase.Paused)
                    CoachStatus = "PAUSED - X TO RESUME\nRIGHT TRIGGER: FLIGHT MENU\nRIGHT STICK CLICK: HUD / B: VIEW\nLEFT MENU: CHARACTERS / A: RESTART + CALIBRATE"
                        + (VoarVR.Telemetry.FlightTelemetry.DefaultEnabled ? "\nMARK: BOTH GRIPS + LEFT STICK CLICK" : "");
                else if (Controller.State.Phase == FlightPhase.Perched)
                    CoachStatus = Controller.LandingCount != lastLandingCount ? "LANDED - LEFT STICK: WALK / FLAP: FLY" : null;
                else if (Controller.LandingApproach > .1f)
                    CoachStatus = "LANDING - RELAX YOUR WINGS";
                else if (Controller.WindVelocity.y > 2f)
                    CoachStatus = Controller.State.Velocity.y>.3f ? "SOARING +"+Controller.State.Velocity.y.ToString("F1")+" m/s\nRELAX YOUR WINGS · GENTLE CIRCLE" : "RISING AIR · OPEN YOUR WINGS\nEASE THE TURN TO START CLIMBING";
                else if (Controller.WindVelocity.y < -2f)
                    CoachStatus = "DESCENDING DRAFT\nLEAVE THE RED STREAM";
                else CoachStatus = null;
            }
            var presentationFrame = xr != null && !calibration.Captured ? FlightInputFrame.Neutral : frame;
            if (Controller.State.Phase != FlightPhase.Paused)
            {
                groundPresentation?.RestoreBase();
                if (rig != null) rig.Present(presentationFrame, calibration, Controller.ControlMode==FlightControlMode.Acrobatic?Controller.State.Rotation:Heading, deltaTime);
                groundPresentation?.Present(Controller, deltaTime);
                avian?.Present(presentationFrame,calibration,Controller,groundPresentation.GroundBlend,deltaTime);
            }
            feedback?.Tick(deltaTime);
            bool hadSeed = Expedition?.Challenge?.SeedCollected == true;
            bool wasComplete = Expedition?.Challenge?.Status == ChallengeStatus.Completed;
            Expedition?.Tick(deltaTime);
            if (Expedition?.Challenge?.Activity == FlightActivity.RouteHome)
            {
                bool completed = !wasComplete && Expedition.Challenge.Status == ChallengeStatus.Completed;
                if (completed || (!hadSeed && Expedition.Challenge.SeedCollected)) feedback?.StoryCue(completed);
            }
            if (Controller.IsPaused && !lastObservedPaused) { RecordSupportedPerch(); Expedition?.SaveCheckpoint(); }
            lastObservedPaused = Controller.IsPaused;
            GetComponent<VoarVR.Gameplay.SkyForaging>()?.Tick(deltaTime);
            journeyPresentation?.Tick(Controller.State.Position);
            telemetry?.Capture(xr!=null?xr.LastDeviceFrame:frame,deltaTime,xr==null || xr.LastWingsEnabled);
        }

        public static float AdvanceComfortYaw(float current,float target,float dt)=>Mathf.MoveTowardsAngle(current,target,45*dt);

        private string MenuStatus()
        {
            if (recoveryPending) return "Preparing your saved perch.\nYour journey progress is kept.";
            if (resumeNeedsCalibration) return "Your journey is saved.\nHold a comfortable spread and choose RECALIBRATE HERE.\nThen resume when ready.";
            var challenge = Expedition?.Challenge;
            string goal = challenge == null || challenge.Activity == FlightActivity.FreeFlight
                ? "FREE FLIGHT\nExplore, land and leave whenever you like."
                : challenge.Title + "\n" + challenge.Instruction;
            if (Expedition?.Journey != null && !Expedition.Journey.CanWrite)
                goal += "\nSaving is unavailable; your existing save is protected.";
            return goal;
        }

        private void HandleMenuAction(FlightMenuAction action)
        {
            actionGate?.RequireRelease();
            switch (action)
            {
                case FlightMenuAction.Resume:
                    if (recoveryPending || resumeNeedsCalibration) { flightMenu.ShowMessage(MenuStatus()); return; }
                    flightMenu.Close(); Controller.SetPaused(false); break;
                case FlightMenuAction.RecalibrateHere:
                    RecalibrateInPlace(); break;
                case FlightMenuAction.ReturnSafePerch:
                    ReturnToCheckpoint(); break;
                case FlightMenuAction.RestartRoute:
                    RestartAndCalibrate(input is XRFlightInput xr ? xr.LastRawFrame : Controller.LastInput); break;
                case FlightMenuAction.SaveAndLeave:
                    ReturnToCharacterSelect(); break;
                case FlightMenuAction.ToggleHaptics:
                case FlightMenuAction.ToggleAudio:
                case FlightMenuAction.ToggleFirstPersonComfort:
                case FlightMenuAction.ToggleGuidance:
                    feedback?.PreferencesChanged(); break;
            }
        }

        public bool RecalibrateInPlace()
        {
            if (Controller == null || !Controller.IsPaused || recoveryPending) return false;
            var frame = input is XRFlightInput xr ? xr.LastRawFrame : Controller.LastInput;
            if (UsesXR && !CaptureNeutral(frame, "READY TO RESUME"))
            {
                flightMenu?.ShowMessage("Pose not accepted.\nHold both tracked controllers in a comfortable level spread and try again.\nYour journey is unchanged.");
                return false;
            }
            if (!UsesXR) Controller.Calibrate(frame);
            resumeNeedsCalibration = false;
            Expedition?.InvalidateObservation();
            actionGate?.RequireRelease();
            flightMenu?.ShowMessage("Comfortable neutral captured.\nYour position and journey are unchanged.\nChoose RESUME when ready.");
            return true;
        }

        public void ReturnToCheckpoint()
        {
            if (Controller == null || recoveryPending) return;
            RecordSupportedPerch();
            Expedition?.SaveCheckpoint();
            var checkpoint = Expedition?.Journey.GetCheckpoint(Expedition.Challenge.Activity);
            BeginPerchRecovery(checkpoint != null && checkpoint.HasSafePerch ? checkpoint.SafePerch : DeparturePerch(),
                checkpoint == null || !checkpoint.HasSafePerch);
        }

        private LogicalPosition DeparturePerch()
        {
            int seed = world != null ? world.Space.Seed : 7319;
            return new LogicalPosition(originalSpawn.x,
                WorldTerrain.Elevation(seed, originalSpawn.x, originalSpawn.z) + profile.CollisionRadius + FlightContactSolver.Skin,
                originalSpawn.z);
        }

        private void BeginPerchRecovery(LogicalPosition target, bool fallback)
        {
            if (world == null) { flightMenu?.ShowMessage("A loaded perch is not available here."); return; }
            Controller.SetPaused(true);
            recoveryTarget = target; recoveryFallback = fallback; recoveryWait = 0; recoveryPending = true;
            Expedition?.InvalidateObservation();
            GetComponent<SkyForaging>()?.InvalidateSweep();
            actionGate?.RequireRelease();
            var local = world.Space.ToLocal(target.X, target.Y, target.Z);
            if (Mathf.Abs(local.x) > 768 || Mathf.Abs(local.z) > 768)
                RebaseWorld(new Vector3(Mathf.Floor(local.x / 128) * 128, 0, Mathf.Floor(local.z / 128) * 128));
            world.SetRecoveryFocus(world.Space.ToLocal(target.X, target.Y, target.Z));
            flightMenu?.ShowMessage("Preparing your perch.\nYour journey progress is kept.");
        }

        private void ProcessPerchRecovery(float dt)
        {
            if (!recoveryPending || world == null) return;
            recoveryWait += dt;
            var target = world.Space.ToLocal(recoveryTarget.X, recoveryTarget.Y, recoveryTarget.Z);
            world.TickStreaming(target);
            var sky = FindAnyObjectByType<SkyArchipelago>();
            if (world.IsReadyAt(target))
            {
                sky?.Tick(target);
                Physics.SyncTransforms();
                if (Controller.TryRecoverToPerch(target, 0))
                {
                    recoveryPending = false; world.SetRecoveryFocus(null);
                    transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
                    comfortYaw = 0; comfortTransition = false;
                    Expedition?.InvalidateObservation();
                    GetComponent<SkyForaging>()?.InvalidateSweep();
                    GetComponent<BirdAirflowTrails>()?.ClearHistory();
                    RecordSupportedPerch();
                    flightMenu?.ShowMessage(resumeNeedsCalibration ? MenuStatus() : "Your journey is ready.\nRest here, then choose RESUME.\nFlap to leave the perch.");
                    return;
                }
            }
            else if (recoveryWait < 15) return;
            if (!recoveryFallback) { BeginPerchRecovery(DeparturePerch(), true); return; }
            recoveryPending = false; world.SetRecoveryFocus(null);
            flightMenu?.ShowMessage("No clear supported perch was found.\nYou remain paused. Your journey is saved.\nUse RESTART ROUTE to return to the beginning.");
        }

        private void RecordSupportedPerch()
        {
            if (Controller != null && world != null && Controller.HasSupportedPerch)
                Expedition?.RecordSupportedPerch(world.Space.ToLogical(Controller.State.Position));
        }
        private void ShowMode()
        { CoachStatus=Controller.ControlMode==FlightControlMode.Acrobatic?"ACROBATIC FLIGHT\nBANK TO ROLL / TILT BOTH WRISTS TO PITCH\nA: RECOVER / HOLD LEFT STICK: BEGINNER":"BEGINNER FLIGHT";coachUntil=Time.unscaledTime+5; }

        public void ShowTelemetryMarker(int number)
        { CoachStatus="MARK "+number; coachUntil=Time.unscaledTime+1f; }

        // A is the explicit recovery action: restart flight and capture the held neutral.
        public bool RestartAndCalibrate(FlightInputFrame frame)
        {
            recoveryPending = resumeNeedsCalibration = false;
            world?.SetRecoveryFocus(null);
            flightMenu?.Close(); flightMenu?.RequireInputRelease(); actionGate?.RequireRelease();
            if(world!=null) world.ResetOrigin();
            Controller.SetSpawn(originalSpawn);
            Controller.Reset();
            lastTrickCount = lastTrickScore = 0;
            Expedition?.Restart();GetComponent<VoarVR.Gameplay.SkyForaging>()?.InvalidateSweep();
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
            viewMode = FlightViewMode.ThirdPerson;
            platformRecenterPending = false;
            resumeNeedsCalibration = false;
            if (CaptureNeutral(frame, "READY - GLIDE + FLAP\nB: VIEW / X: PAUSE / LEFT MENU: CHARACTERS")) return true;
            calibration.BeginPlatformRecenter();
            if (input is XRFlightInput xr) { xr.WingsEnabled = false; xr.ResetDerivatives(); }
            CalibrationStatus = "Calibration rejected: hold a level comfortable T pose";
            CoachStatus = "HOLD A LEVEL T POSE\nPRESS A: START + CALIBRATE";
            coachUntil = Time.unscaledTime + 3f;
            return false;
        }

        private bool CaptureNeutral(FlightInputFrame frame, string message)
        {
            if (!calibration.CaptureComfortableGlide(frame))
            { telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.CalibrationRejected); return false; }
            telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.CalibrationAccepted);
            Controller.Calibrate(frame);
            if (input is XRFlightInput xr) { xr.WingsEnabled = true; xr.ResetDerivatives(); }
            platformRecenterPending = false;
            CalibrationStatus = $"Calibrated: {calibration.HumanSpanMeters:F2} m span, {calibration.MotionScale:F2} wing scale";
            resumeNeedsCalibration = false;
            CoachStatus = message;
            coachUntil = Time.unscaledTime + 4f;
            return true;
        }

        // OpenXR exposes an origin-change notification, not the reserved Meta button.
        // Wait for fresh, stable tracking after that event before accepting a neutral pose.
        public void NotifyTrackingOriginUpdated()
        {
            if (!UsesXR || Controller == null) return;
            telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.Recenter);
            platformRecenterPending = true;
            flightMenu?.Close(); actionGate?.RequireRelease();
            stableRecenterSeconds = 0f;
            hasRecenterFrame = false;
            calibration.BeginPlatformRecenter();
            if (input is XRFlightInput xr)
            {
                xr.WingsEnabled = false;
                xr.ResetTrackingOrigin();
            }
            CalibrationStatus = "Platform recentered: hold your comfortable spread still";
            CoachStatus = "HOLD COMFORTABLE SPREAD STILL\nRECENTER CALIBRATES HERE";
            coachUntil = float.PositiveInfinity;
        }

        public bool TryCompletePlatformRecenter(FlightInputFrame frame, float deltaTime)
        {
            if (!platformRecenterPending) return false;
            bool comfortable = calibration.IsComfortableGlidePose(frame);
            bool stable = comfortable && hasRecenterFrame
                && Vector3.Distance(frame.HeadPosition, previousRecenterFrame.HeadPosition) < .025f
                && Vector3.Distance(frame.LeftWing.Position, previousRecenterFrame.LeftWing.Position) < .025f
                && Vector3.Distance(frame.RightWing.Position, previousRecenterFrame.RightWing.Position) < .025f
                && Quaternion.Angle(frame.HeadOrientation, previousRecenterFrame.HeadOrientation) < 3f
                && Quaternion.Angle(frame.LeftWing.Orientation, previousRecenterFrame.LeftWing.Orientation) < 5f
                && Quaternion.Angle(frame.RightWing.Orientation, previousRecenterFrame.RightWing.Orientation) < 5f;
            stableRecenterSeconds = stable ? stableRecenterSeconds + Mathf.Min(deltaTime, .05f) : 0f;
            // Compare against the start of the hold window, not the preceding frame:
            // continuous slow motion must not become "still" at high headset frame rates.
            if (!stable) previousRecenterFrame = frame;
            hasRecenterFrame = comfortable;
            return stableRecenterSeconds >= .35f && CaptureNeutral(frame, "WING NEUTRAL RECALIBRATED\nA: START / LEFT MENU: CHARACTERS");
        }

        public void RebaseWorld(Vector3 delta)
        {
            if (world == null) return;
            Controller.RebaseOrigin(delta);
            transform.position -= delta;
            if (Camera.main != null) Camera.main.transform.position -= delta;
            world.Space.Shift(delta);
        }

        public void ReturnToCharacterSelect()
        {
            if (returningToSelection) return;
            RecordSupportedPerch();
            bool journeySaved = Expedition == null || Expedition.SaveCheckpoint();
            var foraging = GetComponent<SkyForaging>();
            bool foragingSaved = foraging == null || foraging.SaveBest();
            if (Expedition?.Journey != null && Expedition.Journey.CanWrite && (!journeySaved || !foragingSaved))
            {
                Controller.SetPaused(true);
                flightMenu?.Open(); flightMenu?.RequireInputRelease(); actionGate?.RequireRelease();
                flightMenu?.ShowMessage("Saving did not finish. Your current journey is still here.\nTry SAVE + LEAVE again when storage is available.");
                return;
            }
            flightMenu?.Close();
            telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.CharacterReturn);
            returningToSelection = true;
            Time.timeScale = 1f;
            CharacterSelection.Chosen = null;
            SceneManager.LoadScene("CharacterSelect");
        }

        private void OnApplicationPause(bool value)
        {
            applicationPaused = value;
            if (!value || Controller == null) return;
            Controller.SetPaused(true); RecordSupportedPerch(); Expedition?.SaveCheckpoint();
            GetComponent<SkyForaging>()?.SaveBest(); flightMenu?.Close(); actionGate?.RequireRelease();
        }

        private void OnApplicationFocus(bool value)
        {
            applicationFocused = value;
            if (value || Controller == null) return;
            Controller.SetPaused(true); RecordSupportedPerch(); Expedition?.SaveCheckpoint();
            GetComponent<SkyForaging>()?.SaveBest(); flightMenu?.Close(); actionGate?.RequireRelease();
        }

        public void ToggleView() =>
            viewMode = viewMode == FlightViewMode.FirstPerson ? FlightViewMode.ThirdPerson : FlightViewMode.FirstPerson;

        public void SetViewMode(FlightViewMode value) => viewMode = value;

        private void OnTrackingOriginUpdated(XRInputSubsystem _) => NotifyTrackingOriginUpdated();

        private void OnDestroy()
        {
            foreach (var subsystem in xrSubsystems) subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            (input as IDisposable)?.Dispose();
        }
    }
}
