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
        private CalibrationCoach calibrationCoach;
        private GameplayRibbon gameplayRibbon;
        private ObstacleCourseDirector obstacleCourse;
        private JourneyPresentation journeyPresentation;
        private SkyRivals skyRivals;
        private enum RecoveryStage { SavedPerch, Departure, SpawnFloor }
        private bool recoveryPending, resumeNeedsCalibration;
        private bool guidedCalibrationPending;
        private bool resumeAfterDepartureRecovery;
        private RecoveryStage recoveryStage;
        private LogicalPosition recoveryTarget;
        private float recoveryWait;
        private bool menuToggle, menuSelect;
        private bool applicationFocused = true, applicationPaused, lastObservedPaused;
        private int menuNavigation;
        private PlayerProfileCatalog playerProfiles;
        private FlightSessionHistoryStore sessionHistory;
        private FlightSessionTracker sessionTracker;
        private FlightSessionSummary finalizedSession;
        private string finalizedResultsText;
        private float sessionDraftElapsed;
        private int sessionCollisions,sessionLandings,sessionTakeoffs;
        public bool RecoveryPending => recoveryPending;
        public bool ResumeNeedsCalibration => resumeNeedsCalibration;
        public BirdTrackingCalibration Calibration => calibration;
        public string CalibrationStatus { get; private set; } = "Spread arms and press A to start + calibrate";
        public string CoachStatus { get; private set; }
        public bool FlightMenuVisible => flightMenu != null && flightMenu.Visible;
        public bool CalibrationCoachVisible => calibrationCoach != null && calibrationCoach.Visible;
        public string ActiveProfileId => playerProfiles?.ActiveProfileId ?? PlayerProfileCatalog.BuiltInProfileId;
        public string ActiveProfileName => playerProfiles?.ActiveProfile?.DisplayName ?? PlayerProfileCatalog.BuiltInProfileName;
        public string CharacterStableId => StableId(character != null ? character.name : CharacterSelection.DefaultCharacterName);
        public ObstacleCourseDirector ObstacleCourse => obstacleCourse;
        public bool CourseTimingAllowed=>applicationFocused&&!applicationPaused;
        public bool FlightOverlayBlocked=>FlightMenuVisible||CalibrationCoachVisible
            || obstacleCourse?.ModalOverlayVisible==true;
        public bool ShowGameplayRibbon=>!FlightOverlayBlocked;
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
        public bool UsesHands => UsesXR && HandInputSettings.UseHands;
        private bool CalibrationLocksFlight => UsesXR && (!calibration.Captured
            || guidedCalibrationPending || platformRecenterPending);
        private bool AuthoritativePause => recoveryPending || resumeNeedsCalibration
            || CalibrationLocksFlight || finalizedSession != null || flightMenu?.ResultsVisible == true
            || obstacleCourse?.RequiresFlightPause == true || !applicationFocused || applicationPaused;
        private bool ResetActionBlocked => guidedCalibrationPending || platformRecenterPending
            || recoveryPending || resumeNeedsCalibration || finalizedSession != null
            || obstacleCourse!=null && !obstacleCourse.AllowsResetAction;

        private void Start()
        {
            viewMode = FlightViewMode.ThirdPerson;
            // Auto chooses XR on device; macOS Editor remains hardware-independent.
            UsesXR = inputMode == FlightInputMode.XR || (inputMode == FlightInputMode.Auto
                && (HandInputSettings.UseHands || Application.platform == RuntimePlatform.Android && !Application.isEditor));
            if (UsesXR)
                input = HandInputSettings.UseHands ? (IFlightInput)new MetaHandFlightInput() : new XRFlightInput();
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
            calibration.CompactHands = UsesHands;
            if (UsesHands) CalibrationStatus = "Spread your hands comfortably, then look at READY and pinch";
            wind = FindAnyObjectByType<WindField>();
            world = FindAnyObjectByType<WorldStreamer>();
            wind?.ConfigureSpecies(profile);
            originalSpawn = transform.position;
            var environment = gameObject.AddComponent<UnityFlightEnvironment>();
            actionGate = new FlightActionGate(input);
            Controller = new BirdFlightController(actionGate, originalSpawn, profile:profile, wind:wind, environment:environment);
            Controller.SpanFlareEnabled = !UsesHands;
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
            SkyForaging foraging=null;
            if(world!=null){foraging=gameObject.AddComponent<VoarVR.Gameplay.SkyForaging>();foraging.Configure(this,world.Space,wind);foraging.Caught+=OnCollectibleCaught;}
            StartSessionTracking();
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
            gameplayRibbon = gameObject.AddComponent<GameplayRibbon>();
            gameplayRibbon.Configure(this, Camera.main);
            gameObject.AddComponent<BirdAirflowTrails>().Configure(this, worldPresentation != null ? worldPresentation.AirflowMaterial : null,
                world != null ? world.Space : null);
            flightMenu = gameObject.AddComponent<FlightMenu>();
            flightMenu.Configure(Camera.main, HandleMenuAction, MenuStatus, MenuActionLabel);
            calibrationCoach = gameObject.AddComponent<CalibrationCoach>();
            calibrationCoach.Configure(Camera.main);
            calibrationCoach.ConfirmRequested += ConfirmHandCalibration;
            if (world != null)
            {
                journeyPresentation = gameObject.AddComponent<JourneyPresentation>();
                journeyPresentation.Configure(world.Space, Expedition);
                gameObject.AddComponent<RouteBearing>().Configure(this, journeyPresentation, Camera.main);
            }
            if(Expedition?.Challenge?.Activity==FlightActivity.ObstacleCourse && world!=null)
            {
                obstacleCourse=gameObject.AddComponent<ObstacleCourseDirector>();
                obstacleCourse.Configure(this,world.Space,foraging);
                flightMenu.SetCourseContext(true);
                gameplayRibbon.SetCourseStatusProvider(()=>obstacleCourse.StatusText);
            }
            else if(world!=null)
            {skyRivals=gameObject.AddComponent<SkyRivals>();skyRivals.Configure(this,world.Space);}
            if (Expedition.ResumeCheckpoint != null)
            {
                resumeNeedsCalibration = UsesXR;
                CalibrationStatus = "Journey resumed: recalibrate here from the flight menu";
                BeginPerchRecovery(Expedition.ResumeCheckpoint.HasSafePerch
                    ? Expedition.ResumeCheckpoint.SafePerch : DeparturePerch(),
                    Expedition.ResumeCheckpoint.HasSafePerch ? RecoveryStage.SavedPerch : RecoveryStage.Departure);
            }
            else if (UsesXR && !calibration.Captured)
            {
                calibrationCoach.Show(false);
                Controller.SetPaused(true);
                // Hands calibrate on the departure perch, so the first view is the bird
                // on the ridge rather than the spawn bowl followed by a cut.
                if (UsesHands && obstacleCourse == null && world != null)
                    BeginPerchRecovery(DeparturePerch(), RecoveryStage.Departure);
            }
            if (UsesXR) Debug.Log(UsesHands
                ? "Hand calibration: spread tracked hands comfortably, look at READY and pinch."
                : "Flight calibration: spread both tracked arms comfortably, then press the right primary button.");
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
            rigDriver.presentationRoot = rigInstance.transform;
            rigDriver.restArmSpan = selected.RestArmSpan;
            rigDriver.Configure();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OpenSessionMenu();
            }
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) GetComponent<VoarVR.UI.FlightHud>()?.Toggle();
            if(Keyboard.current!=null && Keyboard.current.cKey.wasPressedThisFrame) { Controller.SetControlMode(Controller.ControlMode==FlightControlMode.Beginner?FlightControlMode.Acrobatic:FlightControlMode.Beginner); ShowMode(); }
            if (Keyboard.current != null)
            {
                if (Keyboard.current.xKey.wasPressedThisFrame && !AuthoritativePause) Controller.SetPaused(!Controller.IsPaused);
                if (Keyboard.current.bKey.wasPressedThisFrame) ToggleView();
                menuToggle = Keyboard.current.f1Key.wasPressedThisFrame;
                menuSelect = Keyboard.current.enterKey.wasPressedThisFrame;
                menuNavigation = Keyboard.current.upArrowKey.wasPressedThisFrame ? -1
                    : Keyboard.current.downArrowKey.wasPressedThisFrame ? 1 : 0;
            }
            if (Time.deltaTime > 0f) Tick(Mathf.Min(Time.deltaTime, .05f),Time.unscaledDeltaTime);
        }

        // Explicit runtime step also used by scene regression tests and editor evidence capture.
        public void Tick(float deltaTime)=>Tick(deltaTime,deltaTime);

        private void Tick(float deltaTime,float sessionDeltaTime)
        {
            if (returningToSelection) return;
            ProcessPerchRecovery(deltaTime);
            if (input is SyntheticFlightInput synthetic) synthetic.Gesture = gesture;
            // Once per rendered frame so Input System poses and derivative sampling agree.
            // Tests use explicit fixed steps; presentation delta is capped after editor stalls.
            bool lockPause=AuthoritativePause;
            bool wasSupportedBeforeStep=Controller.HasSupportedPerch
                ||Controller.State.Phase==FlightPhase.Perched;
            Controller.ResetEnabled=!UsesXR && !ResetActionBlocked;
            Controller.PauseEnabled=!lockPause;
            if(input is ITrackedFlightInput resettableInput)resettableInput.ResetEnabled=false;
            if(lockPause)Controller.SetPaused(true);
            Controller.StreamingBlocked = recoveryPending || resumeNeedsCalibration || (world != null && (!world.IsReadyAt(Controller.State.Position)
                || !world.IsReadyAt(Controller.State.Position + Controller.State.Velocity * deltaTime)));
            Controller.Step(deltaTime);
            if (AuthoritativePause) Controller.SetPaused(true);
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
            var xr = input as ITrackedFlightInput;
            var frame = xr != null ? xr.LastRawFrame : Controller.LastInput;
            if(finalizedSession!=null)
            {
                // Results are the only legal state after a committed finish. Controller
                // tracking loss must not dismiss the modal and expose route actions; keep
                // it visible, require a clean release, and restore it if another lifecycle
                // event temporarily disabled its canvas.
                bool resultsInputAvailable=applicationFocused&&!applicationPaused
                    && MenuTrackingAvailable(frame);
                Controller.SetPaused(true);
                if(resultsInputAvailable)
                {
                    if(flightMenu?.ResultsVisible!=true)RestoreFinalizedResults();
                    flightMenu?.HandleInput(frame,true,menuToggle,menuNavigation,menuSelect);
                }
                else
                {flightMenu?.RequireInputRelease();actionGate?.RequireRelease();}
                menuToggle=menuSelect=false;menuNavigation=0;
                rig?.SetPresentationVisible(false);
                return;
            }
            if(frame.ResetPressed && !frame.RecalibratePressed && Controller.ResetEnabled)
            {
                sessionTracker?.MarkResetDiscontinuity();Expedition?.InvalidateObservation();
                GetComponent<SkyForaging>()?.InvalidateSweep();GetComponent<BirdAirflowTrails>()?.ClearHistory();
                obstacleCourse?.RequestSafetyReset();
            }
            if (UsesHands && xr != null) calibrationCoach?.UpdateHands(frame, xr.LastDeviceFrame, deltaTime);
            else calibrationCoach?.UpdateReadiness(frame, calibration);
            if(frame.ControlModePressed)ShowMode();
            if (Controller.Tricks.Count != lastTrickCount)
            {
                if (Controller.Tricks.Count > lastTrickCount)
                {
                    sessionTracker?.RecordTrick(Controller.Tricks.Count-lastTrickCount);
                    CoachStatus = Controller.Tricks.Last.ToString().ToUpperInvariant()+"  +"+Mathf.Max(0, Controller.Tricks.Score-lastTrickScore);
                    coachUntil = Time.unscaledTime + 2;
                    feedback?.TrickCue();
                }
                lastTrickCount = Controller.Tricks.Count;
                lastTrickScore = Controller.Tricks.Score;
            }
            if (frame.HudTogglePressed) GetComponent<VoarVR.UI.FlightHud>()?.Toggle();
            if (frame.CharacterSelectPressed)
            {
                if(guidedCalibrationPending)
                {
                    guidedCalibrationPending=false;
                    calibrationCoach?.Hide();
                    OpenSessionMenu();
                }
                else if(resumeNeedsCalibration){calibrationCoach?.Hide();OpenSessionMenu();}
                else if(platformRecenterPending)
                {
                    // An OpenXR origin update is not a request to abandon the flight.
                    // Keep the preserved-flight recenter coach authoritative until a
                    // fresh stable pose is accepted.
                    calibrationCoach?.Show(true);actionGate?.RequireRelease();
                }
                // Hands pinch READY on this coach: holding both pinches while trying is
                // not a request to abandon the flight (Left Menu keeps that on controllers).
                else if(!calibration.Captured){if(!UsesHands)ReturnToCharacterSelect();}
                else OpenSessionMenu();
            }
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
            if (frame.RecalibratePressed && flightMenu?.ResultsVisible != true)
            {
                if (guidedCalibrationPending) CompleteGuidedCalibration(frame);
                else if (resumeNeedsCalibration) BeginGuidedCalibration();
                else if (!calibration.Captured)
                    RestartAndCalibrate(frame, false);
                else if (UsesXR)
                {
                    if (obstacleCourse?.Runtime?.State == CourseState.Running)
                        obstacleCourse.RequestSafetyReset();
                    else if (obstacleCourse != null)
                        obstacleCourse.RequestRestart();
                    BeginGuidedCalibration();
                }
                else if (!ResetActionBlocked) RestartAndCalibrate(frame);
            }
            else if (platformRecenterPending && !UsesHands) TryCompletePlatformRecenter(frame, deltaTime);
            bool menuInputAvailable = applicationFocused && !applicationPaused
                &&(!CalibrationLocksFlight||resumeNeedsCalibration||flightMenu?.Visible==true)
                && MenuTrackingAvailable(frame);
            // Pinch confirms paused course prompts; it never becomes an airborne tuck.
            var courseFrame = frame;
            if (UsesHands) courseFrame.Tuck = (frame.ButtonsHeld & (512u | 1024u)) != 0 ? 1f : 0f;
            bool courseInputAvailable = menuInputAvailable && (!UsesHands
                || MeasuredHandMotion(frame.LeftWing) && MeasuredHandMotion(frame.RightWing));
            bool courseConsumed=obstacleCourse!=null && !FlightMenuVisible
                && obstacleCourse.HandleInput(courseFrame,menuSelect,courseInputAvailable);
            if (menuInputAvailable)
            {
                if(!courseConsumed)
                    flightMenu?.HandleInput(frame, Controller.IsPaused, menuToggle, menuNavigation, menuSelect);
                else { flightMenu?.Close(); flightMenu?.RequireInputRelease(); }
            }
            else
            {
                // Hands naturally leave tracking while arms rest. Keep the card readable;
                // fresh trusted input still requires a release before any action.
                if (!UsesHands) flightMenu?.Close();
                flightMenu?.RequireInputRelease(); actionGate?.RequireRelease();
            }
            menuToggle = menuSelect = false; menuNavigation = 0;
            if (returningToSelection) return;
            // Hands have no pause button: a pause with nothing to look at (focus return,
            // headset removal, an interrupted departure) always shows the rest card.
            if (UsesHands && obstacleCourse == null && Controller.IsPaused && !AuthoritativePause
                && !FlightMenuVisible && !CalibrationCoachVisible) OpenSessionMenu();
            // Cards draw over the world. The calibration coach sits above the bird so the
            // player sees their bird while fitting the pose; rest and course cards hide it.
            rig?.SetPresentationVisible(!FlightMenuVisible && obstacleCourse?.ModalOverlayVisible != true);
            if (Controller.LandingCount != lastLandingCount)
            {
                lastLandingCount=Controller.LandingCount;
                RecordSupportedPerch();
                CoachStatus=UsesHands ? "LANDED · REST YOUR ARMS · FLAP TO FLY" : "LANDED - LEFT STICK: WALK · RIGHT STICK: TURN · FLAP: FLY"; coachUntil=Time.unscaledTime+3f;
            }
            if (!platformRecenterPending && Time.unscaledTime >= coachUntil)
            {
                if (Controller.State.Phase == FlightPhase.Paused)
                    CoachStatus = UsesHands ? "PAUSED · LOOK AT A MENU ACTION AND PINCH\nHOLD BOTH PINCHES IN FRONT TO OPEN THE MENU" : "PAUSED - X TO RESUME\nRIGHT TRIGGER: FLIGHT MENU\nRIGHT STICK CLICK: HUD / B: VIEW\nLEFT MENU: SESSION / A: CALIBRATION COACH"
                        + (VoarVR.Telemetry.FlightTelemetry.DefaultEnabled ? "\nMARK: BOTH GRIPS + LEFT STICK CLICK" : "");
                else if (Controller.State.Phase == FlightPhase.Perched)
                    CoachStatus = Controller.LandingCount != lastLandingCount ? "LANDED - LEFT STICK: WALK · RIGHT STICK: TURN · FLAP: FLY" : null;
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
                groundPresentation?.Present(Controller, deltaTime, GroundArmFold(presentationFrame));
                avian?.Present(presentationFrame,calibration,Controller,groundPresentation.FoldBlend,deltaTime);
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
            if (Controller.IsPaused && !lastObservedPaused) { RecordSupportedPerch(); Expedition?.SaveCheckpoint(); SaveSessionDraft(); }
            lastObservedPaused = Controller.IsPaused;
            GetComponent<VoarVR.Gameplay.SkyForaging>()?.Tick(deltaTime);
            obstacleCourse?.Tick(deltaTime,frame,sessionDeltaTime);
            skyRivals?.Tick(deltaTime);
            journeyPresentation?.Tick(Controller.State.Position);
            telemetry?.Capture(xr!=null?xr.LastDeviceFrame:frame,deltaTime,xr==null || xr.LastWingsEnabled);
            UpdateSessionTracking(frame,sessionDeltaTime,xr==null||xr.LastWingsEnabled,
                wasSupportedBeforeStep);
        }

        public static float AdvanceComfortYaw(float current,float target,float dt)=>Mathf.MoveTowardsAngle(current,target,45*dt);

        private string MenuStatus()
        {
            if (recoveryPending) return "Preparing your saved perch.\nYour journey progress is kept.";
            if (resumeNeedsCalibration) return "Your journey is saved.\nHold a comfortable spread and choose RECALIBRATE HERE.\nThen resume when ready.";
            if (obstacleCourse != null)
                return obstacleCourse.StatusText + (obstacleCourse.Runtime?.State == CourseState.Running
                    ? "\nFollow the gold path and chevrons to the highlighted task."
                    : "\nReturn to the course prompt, or FINISH SESSION to see your movement summary.");
            var challenge = Expedition?.Challenge;
            string goal = challenge == null || challenge.Activity == FlightActivity.FreeFlight
                ? "FREE FLIGHT\nExplore, land and leave whenever you like."
                : challenge.Title + "\n" + challenge.Instruction;
            if (Expedition?.Journey != null && !Expedition.Journey.CanWrite)
                goal += "\nSaving is unavailable; your existing save is protected.";
            return goal;
        }

        private string MenuActionLabel(FlightMenuAction action)
        {
            switch (action)
            {
                case FlightMenuAction.ToggleView: return "View: " + (viewMode == FlightViewMode.FirstPerson ? "first-person" : "third-person");
                case FlightMenuAction.ToggleControlMode: return "Controls: " + (Controller.ControlMode == FlightControlMode.Beginner ? "Beginner" : "Acrobatic");
                case FlightMenuAction.ToggleWind: return "Wind: " + WindModeName;
                case FlightMenuAction.ToggleHud: return "Instruments: " + (GetComponent<FlightHud>()?.Visible == true ? "shown" : "hidden");
                default: return null;
            }
        }

        private void HandleMenuAction(FlightMenuAction action)
        {
            actionGate?.RequireRelease();
            if(finalizedSession!=null&&action!=FlightMenuAction.ReturnAfterResults)
            {Controller?.SetPaused(true);RestoreFinalizedResults();return;}
            switch (action)
            {
                case FlightMenuAction.Resume:
                    if (recoveryPending || resumeNeedsCalibration || CalibrationLocksFlight
                        || flightMenu?.ResultsVisible==true) { flightMenu.ShowMessage(MenuStatus()); return; }
                    if(obstacleCourse?.RequiresFlightPause==true){flightMenu.Close();break;}
                    flightMenu.Close(); Controller.SetPaused(false); break;
                case FlightMenuAction.RecalibrateHere:
                    if (UsesXR) BeginGuidedCalibration(); else RecalibrateInPlace();
                    break;
                case FlightMenuAction.ReturnSafePerch:
                    ReturnToCheckpoint(); break;
                case FlightMenuAction.RestartRoute:
                    if(obstacleCourse!=null)
                    {
                        if(obstacleCourse.RequestRestart())flightMenu.Close();
                        else flightMenu.ShowMessage("Finish the result and rest steps before retrying this course.");
                    }
                    else RestartAndCalibrate(input is ITrackedFlightInput xr ? xr.LastRawFrame : Controller.LastInput);
                    break;
                case FlightMenuAction.FinishSession:
                    FinishSession(); break;
                case FlightMenuAction.ReturnAfterResults:
                case FlightMenuAction.ReturnToSelection:
                    ReturnToCharacterSelect(); break;
                case FlightMenuAction.ToggleView:
                    ToggleView(); break;
                case FlightMenuAction.ToggleHud:
                    GetComponent<FlightHud>()?.Toggle(); break;
                case FlightMenuAction.ToggleControlMode:
                    Controller.SetControlMode(Controller.ControlMode == FlightControlMode.Beginner
                        ? FlightControlMode.Acrobatic : FlightControlMode.Beginner);
                    ShowMode(); break;
                case FlightMenuAction.ToggleWind:
                    if (wind != null)
                    {
                        wind.CycleMode();
                        CoachStatus = "WIND: " + wind.ModeName.ToUpperInvariant();
                        coachUntil = Time.unscaledTime + 3f;
                    }
                    break;
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
            var frame = input is ITrackedFlightInput xr ? xr.LastRawFrame : Controller.LastInput;
            if (UsesXR && !CaptureNeutral(frame, "READY TO RESUME"))
            {
                flightMenu?.ShowMessage(UsesHands
                    ? "Pose not accepted.\nHold both tracked hands in a comfortable level spread and try again.\nYour journey is unchanged."
                    : "Pose not accepted.\nHold both tracked controllers in a comfortable level spread and try again.\nYour journey is unchanged.");
                return false;
            }
            if (!UsesXR) Controller.Calibrate(frame);
            resumeNeedsCalibration = false;
            Expedition?.InvalidateObservation();
            actionGate?.RequireRelease();
            flightMenu?.ShowMessage("Comfortable neutral captured.\nYour position and journey are unchanged.\nChoose "
                + (obstacleCourse != null ? "RETURN TO COURSE" : "CONTINUE FLYING") + " when ready.");
            return true;
        }

        private void ConfirmHandCalibration()
        {
            if (!UsesHands || input is not ITrackedFlightInput tracked || !applicationFocused
                || applicationPaused || recoveryPending || finalizedSession != null) return;
            // Raised by the hands-free countdown: capture its latest frame, seen or inferred.
            var frame = tracked.LastRawFrame;
            if (!frame.HeadTracked || !HandCalibrationFlow.HandVisible(frame.LeftWing)
                || !HandCalibrationFlow.HandVisible(frame.RightWing)) return;
            var flow = calibrationCoach.Flow;
            if (input is MetaHandFlightInput hands && flow.HasContext)
                hands.SetInferredBias(flow.LeftBiasKnown, flow.LeftInferredBias, flow.RightBiasKnown, flow.RightInferredBias);
            if (guidedCalibrationPending) CompleteGuidedCalibration(frame);
            else if (platformRecenterPending || resumeNeedsCalibration)
            {
                guidedCalibrationPending = true;
                CompleteGuidedCalibration(frame);
            }
            else if (!calibration.Captured)
            {
                // A fresh session already rests on its departure perch: calibrate in place
                // instead of resetting the streamed world and re-perching.
                if (Controller.HasSupportedPerch && obstacleCourse == null
                    && CaptureNeutral(frame, "GLIDING IN YOUR NEUTRAL · FLAP DOWN TO CLIMB"))
                {
                    // Glide off the ridge straight away so the new neutral is felt in the air.
                    actionGate?.RequireRelease();
                    Controller.SetPaused(false);
                    Controller.LaunchGlide();
                    CoachStatus = "GLIDING IN YOUR NEUTRAL · FLAP DOWN TO CLIMB";
                    coachUntil = Time.unscaledTime + 6f;
                }
                else RestartAndCalibrate(frame, false);
            }
        }

        private void BeginGuidedCalibration()
        {
            if (Controller == null || recoveryPending || finalizedSession != null
                || sessionTracker?.IsFinished == true) return;
            Controller.SetPaused(true);
            guidedCalibrationPending = true;
            if (input is ITrackedFlightInput xr) xr.ResetEnabled = false;
            flightMenu?.Close();
            flightMenu?.RequireInputRelease();
            actionGate?.RequireRelease();
            calibrationCoach?.Show(true);
            CoachStatus = null;
        }

        private bool CompleteGuidedCalibration(FlightInputFrame frame)
        {
            if (!guidedCalibrationPending || Controller == null || !Controller.IsPaused
                || finalizedSession != null || sessionTracker?.IsFinished == true) return false;
            if (!CaptureNeutral(frame, "COMFORTABLE NEUTRAL CAPTURED"))
            {
                CalibrationStatus = UsesHands ? "Calibration rejected: relax into the illustrated compact spread"
                    : "Calibration rejected: relax into the illustrated T pose";
                calibrationCoach?.UpdateReadiness(frame, calibration);
                return false;
            }
            guidedCalibrationPending = false;
            if (input is ITrackedFlightInput xr) xr.ResetEnabled = false;
            calibrationCoach?.Hide();
            if (UsesHands)
            {
                // The countdown already confirmed readiness: carry on flying in the new neutral.
                actionGate?.RequireRelease();
                if (!AuthoritativePause && !FlightMenuVisible) Controller.SetPaused(false);
                CoachStatus = "NEUTRAL SET · FLAP DOWN TO CLIMB"; coachUntil = Time.unscaledTime + 4f;
                return true;
            }
            flightMenu?.Open();
            flightMenu?.RequireInputRelease();
            flightMenu?.ShowMessage("Comfortable neutral captured.\nYour position and journey are unchanged.\nChoose CONTINUE FLYING when ready.");
            actionGate?.RequireRelease();
            return true;
        }

        public void ReturnToCheckpoint()
        {
            if (Controller == null || recoveryPending) return;
            if(obstacleCourse!=null)
            {
                if (obstacleCourse.Runtime?.State == CourseState.Running)
                    obstacleCourse.RequestSafetyReset();
                else
                    obstacleCourse.RequestRestart();
                PrepareCourseAttempt();
                Controller.SetPaused(true);
                flightMenu?.Close();
                return;
            }
            RecordSupportedPerch();
            Expedition?.SaveCheckpoint();
            var checkpoint = Expedition?.Journey.GetCheckpoint(Expedition.Challenge.Activity);
            bool saved = checkpoint != null && checkpoint.HasSafePerch;
            BeginPerchRecovery(saved ? checkpoint.SafePerch : DeparturePerch(),
                saved ? RecoveryStage.SavedPerch : RecoveryStage.Departure);
        }

        // The ridge lookout gives a supported start an open view toward the first lift.
        // The reliably clear spawn floor remains the last resort if it cannot be supported.
        private LogicalPosition DeparturePerch() => GroundPerch(FlightRegions.DepartureLookoutX, FlightRegions.DepartureLookoutZ);
        private LogicalPosition SpawnFloorPerch() => GroundPerch(originalSpawn.x, originalSpawn.z);
        private LogicalPosition GroundPerch(double x, double z)
        {
            int seed = world != null ? world.Space.Seed : 7319;
            return new LogicalPosition(x, WorldTerrain.Elevation(seed, x, z) + profile.CollisionRadius + FlightContactSolver.Skin, z);
        }

        private void BeginPerchRecovery(LogicalPosition target, RecoveryStage stage)
        {
            resumeAfterDepartureRecovery = false;
            Controller.SetPaused(true);
            if (world == null) { flightMenu?.Open(); flightMenu?.ShowMessage("A loaded perch is not available here."); return; }
            recoveryTarget = target; recoveryStage = stage; recoveryWait = 0; recoveryPending = true;
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
                if (Controller.TryRecoverToPerch(target,
                        recoveryStage == RecoveryStage.Departure ? FlightRegions.DepartureLookoutHeading : 0))
                {
                    recoveryPending = false; world.SetRecoveryFocus(null);
                    transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
                    comfortYaw = 0; comfortTransition = false;
                    // Cards placed before the move would be left behind in the world. A hands
                    // resume has no pause button, so it goes straight to its READY coach.
                    if (UsesHands && resumeNeedsCalibration && !FlightMenuVisible && !CalibrationCoachVisible)
                        calibrationCoach?.Show(true);
                    calibrationCoach?.Reanchor(); flightMenu?.Reanchor();
                    Expedition?.InvalidateObservation();
                    GetComponent<SkyForaging>()?.InvalidateSweep();
                    GetComponent<BirdAirflowTrails>()?.ClearHistory();
                    RecordSupportedPerch();
                    if (resumeAfterDepartureRecovery)
                    {
                        resumeAfterDepartureRecovery = false;
                        if (!FlightMenuVisible && applicationFocused && !applicationPaused
                            && !CalibrationLocksFlight && finalizedSession == null) Controller.SetPaused(false);
                        CoachStatus = "REST HERE · SPREAD YOUR HANDS AND FLAP TO TAKE OFF";
                        coachUntil = Time.unscaledTime + 6f;
                    }
                    flightMenu?.ShowMessage(resumeNeedsCalibration ? MenuStatus() : "Your journey is ready.\nRest here, then choose CONTINUE FLYING.\nFlap to leave the perch.");
                    return;
                }
            }
            else if (recoveryWait < 15) return;
            if (recoveryStage != RecoveryStage.SpawnFloor)
            {
                // A fallback continues the same departure, including its automatic resume.
                bool resume = resumeAfterDepartureRecovery;
                if (recoveryStage == RecoveryStage.SavedPerch) BeginPerchRecovery(DeparturePerch(), RecoveryStage.Departure);
                else BeginPerchRecovery(SpawnFloorPerch(), RecoveryStage.SpawnFloor);
                resumeAfterDepartureRecovery = resume && recoveryPending;
                return;
            }
            recoveryPending = resumeAfterDepartureRecovery = false; world.SetRecoveryFocus(null);
            // Hands have no pause button: keep the choices visible for gaze and pinch.
            if (UsesHands && !FlightMenuVisible) flightMenu?.Open();
            flightMenu?.ShowMessage("No clear supported perch was found.\nYou remain paused. Your journey is saved.\nUse RESTART ROUTE to return to the beginning.");
        }

        private void RecordSupportedPerch()
        {
            if (Controller != null && world != null && Controller.HasSupportedPerch)
                Expedition?.RecordSupportedPerch(world.Space.ToLogical(Controller.State.Position));
        }
        private void ShowMode()
        { CoachStatus=Controller.ControlMode==FlightControlMode.Acrobatic
            ? UsesHands ? "ACROBATIC FLIGHT\nBANK TO ROLL · TILT BOTH WRISTS TO PITCH\nFLIGHT SETTINGS: CHANGE CONTROLS"
                : "ACROBATIC FLIGHT\nBANK TO ROLL / TILT BOTH WRISTS TO PITCH\nA: CALIBRATION COACH / HOLD LEFT STICK: BEGINNER"
            : "BEGINNER FLIGHT";coachUntil=Time.unscaledTime+5; }

        public void ShowTelemetryMarker(int number)
        { CoachStatus="MARK "+number; coachUntil=Time.unscaledTime+1f; }

        // Kept for menu recovery and deterministic tests. Quest A opens the in-place coach.
        public bool RestartAndCalibrate(FlightInputFrame frame)=>RestartAndCalibrate(frame,true);

        private bool RestartAndCalibrate(FlightInputFrame frame,bool resetCourse)
        {
            guidedCalibrationPending = false;
            if (input is ITrackedFlightInput resettable) resettable.ResetEnabled = false;
            recoveryPending = resumeNeedsCalibration = false;
            world?.SetRecoveryFocus(null);
            flightMenu?.Close(); flightMenu?.RequireInputRelease(); actionGate?.RequireRelease();
            if(world!=null) world.ResetOrigin();
            Controller.SetSpawn(originalSpawn);
            Controller.Reset();
            sessionTracker?.MarkResetDiscontinuity();
            lastTrickCount = lastTrickScore = 0;
            Expedition?.Restart();GetComponent<VoarVR.Gameplay.SkyForaging>()?.InvalidateSweep();
            if(resetCourse)obstacleCourse?.ResetForFreshAttempt();
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
            viewMode = FlightViewMode.ThirdPerson;
            platformRecenterPending = false;
            resumeNeedsCalibration = false;
            if (CaptureNeutral(frame, UsesHands ? "READY · FLAP TO TAKE OFF\nHOLD BOTH PINCHES IN FRONT: MENU"
                : "READY - GLIDE + FLAP\nB: VIEW / X: PAUSE / LEFT MENU: SESSION"))
            {
                calibrationCoach?.Hide();
                if (UsesHands && obstacleCourse == null)
                {
                    BeginPerchRecovery(DeparturePerch(), RecoveryStage.Departure);
                    resumeAfterDepartureRecovery = recoveryPending;
                    // The reset streamed the origin chunk synchronously; perch in this frame
                    // when possible so the spawn is never rendered between reset and perch.
                    ProcessPerchRecovery(0f);
                }
                return true;
            }
            calibration.BeginPlatformRecenter();
            if (input is ITrackedFlightInput xr) { xr.WingsEnabled = false; xr.ResetDerivatives(); }
            CalibrationStatus = UsesHands ? "Calibration rejected: hold a comfortable compact spread" : "Calibration rejected: hold a level comfortable T pose";
            CoachStatus = UsesHands ? "HOLD A COMFORTABLE SPREAD\nLOOK AT READY AND PINCH" : "HOLD A LEVEL T POSE\nPRESS A: START + CALIBRATE";
            calibrationCoach?.Show(false);
            coachUntil = Time.unscaledTime + 3f;
            return false;
        }

        private bool CaptureNeutral(FlightInputFrame frame, string message)
        {
            if (!(UsesHands ? calibration.CaptureNaturalHands(frame) : calibration.CaptureComfortableGlide(frame)))
            { telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.CalibrationRejected); return false; }
            telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.CalibrationAccepted);
            Controller.Calibrate(frame);
            if (input is ITrackedFlightInput xr) { xr.WingsEnabled = true; xr.ResetDerivatives(); }
            platformRecenterPending = false;
            CalibrationStatus = $"Calibrated: {calibration.HumanSpanMeters:F2} m span, {calibration.MotionScale:F2} wing scale";
            resumeNeedsCalibration = false;
            calibrationCoach?.Hide();
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
            if(finalizedSession!=null)
            {
                Controller.SetPaused(true);RestoreFinalizedResults();actionGate?.RequireRelease();
                return;
            }
            platformRecenterPending = true;
            Controller.SetPaused(true);
            flightMenu?.Close(); actionGate?.RequireRelease();
            stableRecenterSeconds = 0f;
            hasRecenterFrame = false;
            calibration.BeginPlatformRecenter();
            if (input is ITrackedFlightInput xr)
            {
                xr.WingsEnabled = false;
                xr.ResetTrackingOrigin();
            }
            CalibrationStatus = "Platform recentered: hold your comfortable spread still";
            CoachStatus = UsesHands ? "HOLD A COMFORTABLE SPREAD\nLOOK AT READY AND PINCH TO RECALIBRATE HERE"
                : "HOLD COMFORTABLE SPREAD STILL\nRECENTER CALIBRATES HERE";
            calibrationCoach?.Show(true);
            coachUntil = float.PositiveInfinity;
        }

        public bool TryCompletePlatformRecenter(FlightInputFrame frame, float deltaTime)
        {
            if (!platformRecenterPending || UsesHands) return false;
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
            if(stableRecenterSeconds<.35f
                ||!CaptureNeutral(frame,"WING NEUTRAL RECALIBRATED"))return false;
            flightMenu?.Open();flightMenu?.RequireInputRelease();
            flightMenu?.ShowMessage("Comfortable neutral captured.\nYour flight state is preserved. Choose CONTINUE FLYING when ready.");
            actionGate?.RequireRelease();
            return true;
        }

        public void RebaseWorld(Vector3 delta)
        {
            if (world == null) return;
            Controller.RebaseOrigin(delta);
            transform.position -= delta;
            if (Camera.main != null) Camera.main.transform.position -= delta;
            world.Space.Shift(delta);
            sessionTracker?.MarkWorldRebaseContinuity();
        }

        // Used by course retries after their required rest. The captured neutral and selected
        // species are retained; only simulated world position is restarted.
        public void PrepareCourseAttempt()
        {
            if(Controller==null)return;
            if(world!=null)world.ResetOrigin();
            Controller.SetSpawn(originalSpawn);Controller.Reset();
            transform.SetPositionAndRotation(Controller.State.Position,Controller.State.Rotation);
            sessionTracker?.MarkResetDiscontinuity();
            Expedition?.InvalidateObservation();GetComponent<SkyForaging>()?.InvalidateSweep();
            GetComponent<BirdAirflowTrails>()?.ClearHistory();skyRivals?.InvalidatePositions();actionGate?.RequireRelease();
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
                flightMenu?.ShowMessage("Saving did not finish. Your current journey is still here.\nTry FINISH SESSION again when storage is available.");
                return;
            }
            if(sessionTracker!=null && finalizedSession==null && !FinalizeSession("returned-to-select",out _))
            {
                Controller.SetPaused(true);flightMenu?.Open();flightMenu?.RequireInputRelease();actionGate?.RequireRelease();
                flightMenu?.ShowMessage("Your movement summary could not be saved. Your flight is still here.\n"+(sessionHistory?.LastError??"Try again when storage is available."));
                return;
            }
            flightMenu?.Close();
            telemetry?.Record(VoarVR.Telemetry.TelemetryEvent.CharacterReturn);
            returningToSelection = true;
            Time.timeScale = 1f;
            CharacterSelection.Chosen = null;
            SceneManager.LoadScene("CharacterSelect");
        }

        private void OpenSessionMenu()
        {
            if(finalizedSession!=null)
            {Controller?.SetPaused(true);RestoreFinalizedResults();return;}
            if (Controller == null || returningToSelection || flightMenu == null || flightMenu.ResultsVisible
                || CalibrationLocksFlight && !resumeNeedsCalibration) return;
            obstacleCourse?.PrepareForSessionMenu();
            Controller.SetPaused(true);
            RecordSupportedPerch();
            Expedition?.SaveCheckpoint();
            SaveSessionDraft();
            flightMenu.Open();
            flightMenu.RequireInputRelease();
            actionGate?.RequireRelease();
        }

        private void FinishSession()
        {
            if (Controller == null || returningToSelection) return;
            Controller.SetPaused(true);
            RecordSupportedPerch();
            bool protectedFutureSave = Expedition?.Journey != null && !Expedition.Journey.CanWrite;
            bool journeySaved = protectedFutureSave || Expedition == null || Expedition.SaveCheckpoint();
            var foraging = GetComponent<SkyForaging>();
            bool foragingSaved = foraging == null || foraging.SaveBest();
            if (!journeySaved || !foragingSaved)
            {
                flightMenu?.ShowMessage("Saving did not finish. Your flight is still here.\nFree storage, then choose FINISH SESSION again.");
                return;
            }
            if(!FinalizeSession("player-ended",out var summary))
            {flightMenu?.ShowMessage("Your movement summary could not be saved. Your flight is still here.\n"+(sessionHistory?.LastError??"Try again when storage is available."));return;}
            string saveWarning = protectedFutureSave
                ? "Route progress uses a newer save version. That file was preserved without changes."
                : null;
            finalizedResultsText=FormatSessionResults(summary,saveWarning);
            flightMenu?.ShowResults(finalizedResultsText);
            actionGate?.RequireRelease();
        }

        private void OnApplicationPause(bool value)
        {
            applicationPaused = value;
            if(Controller==null)return;
            if(!value)
            {
                if(finalizedSession!=null){Controller.SetPaused(true);RestoreFinalizedResults();}
                return;
            }
            obstacleCourse?.HandleApplicationSuspended();
            Controller.SetPaused(true); RecordSupportedPerch(); Expedition?.SaveCheckpoint();
            GetComponent<SkyForaging>()?.SaveBest();SaveSessionDraft(); flightMenu?.Close(); actionGate?.RequireRelease();
        }

        private void OnApplicationFocus(bool value)
        {
            applicationFocused = value;
            if(Controller==null)return;
            if(value)
            {
                if(finalizedSession!=null){Controller.SetPaused(true);RestoreFinalizedResults();}
                return;
            }
            obstacleCourse?.HandleApplicationSuspended();
            Controller.SetPaused(true); RecordSupportedPerch(); Expedition?.SaveCheckpoint();
            GetComponent<SkyForaging>()?.SaveBest();SaveSessionDraft(); flightMenu?.Close(); actionGate?.RequireRelease();
        }

        private void RestoreFinalizedResults()
        {
            if(finalizedSession==null)return;
            if(string.IsNullOrEmpty(finalizedResultsText))
                finalizedResultsText=FormatSessionResults(finalizedSession,null);
            flightMenu?.ShowResults(finalizedResultsText);actionGate?.RequireRelease();
        }

        public void ToggleView() =>
            viewMode = viewMode == FlightViewMode.FirstPerson ? FlightViewMode.ThirdPerson : FlightViewMode.FirstPerson;

        public void SetViewMode(FlightViewMode value) => viewMode = value;

        private void OnTrackingOriginUpdated(XRInputSubsystem _) => NotifyTrackingOriginUpdated();

        private void OnDestroy()
        {
            var forage=GetComponent<SkyForaging>();if(forage!=null)forage.Caught-=OnCollectibleCaught;
            if(finalizedSession==null)SaveSessionDraft();
            foreach (var subsystem in xrSubsystems) subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            (input as IDisposable)?.Dispose();
        }

        private void StartSessionTracking()
        {
            playerProfiles=new PlayerProfileCatalog();sessionHistory=new FlightSessionHistoryStore();
            var challenge=Expedition?.Challenge;var course=challenge?.Activity==FlightActivity.ObstacleCourse
                ?CourseCatalog.Find(ActivitySelection.ChosenCourseId):null;
            var context=new FlightSessionContext
            {
                SessionId="s-"+Guid.NewGuid().ToString("N"),ProfileId=playerProfiles.ActiveProfileId,
                StartedUtc=DateTime.UtcNow.ToString("o"),BuildId=StableId(string.IsNullOrEmpty(Application.version)?"development":Application.version),
                CharacterId=CharacterStableId,ActivityId=StableId((challenge?.Activity??FlightActivity.FreeFlight).ToString()),
                ContentId=course?.StableId??challenge?.ContentId??"free-flight.v1",
                ContentRevision=course?.ContentRevision??1,ScoringRevision=course?.ScoringRevision??1
            };
            sessionTracker=new FlightSessionTracker(context);sessionHistory.SaveDraft(sessionTracker.Snapshot());
            sessionCollisions=Controller?.CollisionCount??0;sessionLandings=Controller?.LandingCount??0;sessionTakeoffs=Controller?.TakeoffCount??0;
        }

        private void UpdateSessionTracking(FlightInputFrame frame,float dt,bool wingsEnabled,
            bool wasSupportedBeforeStep)
        {
            if(sessionTracker==null||sessionTracker.IsFinished||Controller==null)return;
            bool tracked=!UsesXR||frame.HeadTracked&&frame.LeftWing.Tracked&&frame.RightWing.Tracked&&wingsEnabled;
            FlightSessionTimeCategory category;
            if(CalibrationLocksFlight||recoveryPending)category=FlightSessionTimeCategory.Excluded;
            else if(Controller.IsPaused)category=FlightSessionTimeCategory.Paused;
            else if(Controller.HasSupportedPerch||Controller.State.Phase==FlightPhase.Perched)category=FlightSessionTimeCategory.SupportedPerch;
            else if(!tracked||Controller.StreamingBlocked)category=FlightSessionTimeCategory.Excluded;
            else category=FlightSessionTracker.ClassifyAirborneHands(frame.LeftWing.Velocity,frame.RightWing.Velocity,
                !MeasuredHandMotion(frame.LeftWing),!MeasuredHandMotion(frame.RightWing));
            bool supportedAfterStep=Controller.State.Phase==FlightPhase.Perched
                ||Controller.HasSupportedPerch;
            // A landing/takeoff transition frame contains airborne displacement. Only
            // classify walking when the bird was supported for the whole simulation step.
            var movement=Controller.IsPaused?FlightSessionMovement.None:
                wasSupportedBeforeStep&&supportedAfterStep?FlightSessionMovement.Walking:
                FlightSessionMovement.Flight;
            bool movementActive=movement==FlightSessionMovement.Walking
                &&(frame.GroundMove.sqrMagnitude>=.04f||Mathf.Abs(frame.GroundTurn)>=.2f);
            var logical=world!=null?world.Space.ToLogical(Controller.State.Position):new LogicalPosition(
                Controller.State.Position.x,Controller.State.Position.y,Controller.State.Position.z);
            sessionTracker.Observe(new FlightSessionObservation{DeltaTime=dt,LogicalPosition=logical,Velocity=Controller.State.Velocity,
                WindVelocity=Controller.WindVelocity,LeftHandVelocity=frame.LeftWing.Velocity,RightHandVelocity=frame.RightWing.Velocity,
                LeftHandMotionEstimated=!MeasuredHandMotion(frame.LeftWing),RightHandMotionEstimated=!MeasuredHandMotion(frame.RightWing),
                TimeCategory=category,Movement=movement,MovementActive=movementActive,TrackingAvailable=tracked,
                StreamingReady=!Controller.StreamingBlocked,SampleValid=true});
            RecordCounter(Controller.CollisionCount,ref sessionCollisions,sessionTracker.RecordCollision);
            RecordCounter(Controller.LandingCount,ref sessionLandings,sessionTracker.RecordLanding);
            RecordCounter(Controller.TakeoffCount,ref sessionTakeoffs,sessionTracker.RecordTakeoff);
            sessionDraftElapsed += dt;
            if (sessionDraftElapsed >= 30f) SaveSessionDraft(false);
        }

        // Perched hands mirror the player's arms: relaxed arms fold the wings, arms opened
        // toward the calibrated span spread them. Controllers keep the full ground fold.
        private float GroundArmFold(FlightInputFrame frame)
        {
            if (!UsesHands || !calibration.Captured || !frame.LeftWing.Tracked || !frame.RightWing.Tracked) return 1f;
            return Mathf.InverseLerp(.85f, .45f, Controller.SpanRatio);
        }

        private bool MenuTrackingAvailable(FlightInputFrame frame) => !UsesXR || frame.HeadTracked
            && (UsesHands ? MeasuredHandMotion(frame.LeftWing) || MeasuredHandMotion(frame.RightWing)
                : frame.LeftWing.Tracked && frame.RightWing.Tracked);

        private static bool MeasuredHandMotion(WingInput wing) => wing.Tracked && !wing.MotionEstimated
            && (wing.Source == HandPoseSource.Unknown || wing.Source == HandPoseSource.DirectHigh);

        private static void RecordCounter(int current,ref int previous,Func<int,bool> record)
        {if(current>previous)record(current-previous);previous=current;}
        private void OnCollectibleCaught(CatchResult result)
        {if(result.IsValid)sessionTracker?.RecordCollectible(result.CollectibleId,result.AwardedValue);}
        private void SaveSessionDraft(bool durable = true)
        {
            if (sessionTracker != null && !sessionTracker.IsFinished)
                sessionHistory?.SaveDraft(sessionTracker.Snapshot(), durable);
            sessionDraftElapsed = 0;
        }
        private bool FinalizeSession(string reason,out FlightSessionSummary summary)
        {
            summary=finalizedSession;
            if(summary==null && sessionTracker!=null)
            {
                var candidate=sessionTracker.PrepareFinish(reason,DateTime.UtcNow);
                if(sessionHistory==null||!sessionHistory.Finalize(candidate))return false;
                if(!sessionTracker.CommitFinish(candidate))return false;
                summary=sessionTracker.Snapshot();
                finalizedSession=summary;
                return true;
            }
            if(summary==null)return true;
            if(sessionHistory==null||!sessionHistory.Finalize(summary))return false;
            finalizedSession=summary;return true;
        }

        private string FormatSessionResults(FlightSessionSummary summary, string saveWarning = null)
        {
            var m = summary.Metrics;
            var previous = sessionHistory.LoadFinals(summary.Context.ProfileId)
                .Where(s => s.Context.SessionId != summary.Context.SessionId).ToArray();
            string historyWarning = sessionHistory.LastError;
            var records = new List<string>();
            if (string.IsNullOrEmpty(historyWarning))
            {
                AddRecord(records, "ACTIVE", ActiveMovementSeconds(m), previous, s => ActiveMovementSeconds(s.Metrics));
                AddRecord(records, "WINGBEATS", m.EstimatedWingbeats, previous, s => s.Metrics.EstimatedWingbeats);
                AddRecord(records, "MOTHS", m.CatchCount, previous, s => s.Metrics.CatchCount);
                AddRecord(records, "VALUE", m.CollectibleValue, previous, s => s.Metrics.CollectibleValue);
                AddRecord(records, "DISTANCE", m.FlightDistanceMeters, previous, s => s.Metrics.FlightDistanceMeters);
                AddRecord(records, "CLIMB", m.GrossAscentMeters, previous, s => s.Metrics.GrossAscentMeters);
                AddRecord(records, "GROUND SPEED", m.MaxGroundSpeedMps, previous, s => s.Metrics.MaxGroundSpeedMps);
                AddRecord(records, "AIR SPEED", m.MaxAirSpeedMps, previous, s => s.Metrics.MaxAirSpeedMps);
                AddRecord(records, "TRICKS", m.TrickCount, previous, s => s.Metrics.TrickCount);
                AddRecord(records, "LANDINGS", m.LandingCount, previous, s => s.Metrics.LandingCount);
            }
            string title = (Expedition?.Challenge?.Title ?? "FREE FLIGHT") + "  ·  " + ActiveProfileName.ToUpperInvariant();
            double restSeconds = Math.Max(0, m.SupportedPerchSeconds - m.WalkingActiveSeconds) + m.PausedSeconds;
            string primary="<color=#F9C96E>"+title.ToUpperInvariant()+"</color>"
                +"\n\nMOVEMENT"
                +"\nACTIVE  "+Duration(ActiveMovementSeconds(m))
                +"\nREST / BREAK  "+Duration(restSeconds)
                +"\nFLIGHT  "+Distance(m.FlightDistanceMeters)
                +"\nCLIMBED  "+Distance(m.GrossAscentMeters)
                +"\nTOP SPEED  "+m.MaxGroundSpeedMps.ToString("F1")+" m/s"
                +"\n\nCOLLECTED"
                +"\nMOTHS  "+m.CatchCount+"  •  VALUE  "+m.CollectibleValue;
            string details="TIME & CONTROL"
                +"\nFLAPPING  "+Duration(m.ActiveHandSeconds)
                +"\nWALKING / TURNING  "+Duration(m.WalkingActiveSeconds)
                +"\nQUIET AIR  "+Duration(m.QuietAirborneSeconds)
                +"\nSUPPORTED  "+Duration(m.SupportedPerchSeconds)+"  •  PAUSED  "+Duration(m.PausedSeconds)
                +"\n\nDETAILS"
                +"\nWINGBEATS  ~"+m.EstimatedWingbeats
                +"\nWALKED  "+Distance(m.WalkingDistanceMeters)
                +"\nDESCENDED  "+Distance(m.GrossDescentMeters)
                +"\nAIR SPEED  "+m.MaxAirSpeedMps.ToString("F1")+" m/s"
                +"\nTRICKS  "+m.TrickCount+"  •  LANDINGS  "+m.LandingCount+"  •  CONTACTS  "+m.CollisionCount;
            if(records.Count>0)details+="\n\n<color=#F9C96E>NEW RECORDS  "+string.Join(" · ",records)+"</color>";
            if(!string.IsNullOrEmpty(historyWarning))details+="\nHISTORY WARNING  "+historyWarning;
            if(!string.IsNullOrEmpty(saveWarning))details+="\nSAVE WARNING  "+saveWarning;
            details+="\n\nSaved locally to this player's history.";
            return primary+FlightMenu.ResultColumnSeparator+details;
        }
        private static double ActiveMovementSeconds(FlightSessionMetrics metrics)=>
            metrics.ActiveHandSeconds+metrics.WalkingActiveSeconds;
        private static void AddRecord(List<string> records, string label, double value,
            FlightSessionSummary[] previous, Func<FlightSessionSummary, double> read)
        {
            if (value <= 0) return;
            if (previous.Length == 0 || value > previous.Max(read) + .00001) records.Add(label);
        }
        private static string Duration(double seconds)=>((int)seconds/60)+":"+((int)seconds%60).ToString("00");
        private static string Distance(double metres)=>metres>=1000?(metres/1000).ToString("F2")+" km":Math.Round(metres)+" m";
        private static string StableId(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return "unknown";var chars=value.ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)?c:'-').ToArray();
            string id=new string(chars).Trim('-');while(id.Contains("--"))id=id.Replace("--","-");return string.IsNullOrEmpty(id)?"unknown":id;
        }
    }
}
