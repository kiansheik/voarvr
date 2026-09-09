using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using VoarVR.Input;
using VoarVR.World;

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
        [SerializeField] private FlightViewMode viewMode = FlightViewMode.FirstPerson;
        private BirdRigDriver rig;
        private BirdCharacterDefinition character;
        private WindField wind;
        private readonly List<XRInputSubsystem> xrSubsystems = new List<XRInputSubsystem>();
        private bool platformRecenterPending;
        private float stableRecenterSeconds;
        private FlightInputFrame previousRecenterFrame;
        private bool hasRecenterFrame;
        private bool returningToSelection;
        private float coachUntil;
        public BirdTrackingCalibration Calibration => calibration;
        public string CalibrationStatus { get; private set; } = "Spread arms and press A to start + calibrate";
        public string CoachStatus { get; private set; }
        public FlightViewMode ViewMode => viewMode;
        public float PhysicalYawOffsetDeg => Controller != null ? Controller.PhysicalYawOffsetDeg : 0f;
        public float CharacterRestHalfSpan => character != null ? character.RestArmSpan : .56f;
        public Vector3 CameraEyeAnchor => BirdTrackingCalibration.EyeAnchor
            + Vector3.up * (Mathf.Max(0f, CharacterRestHalfSpan - .56f) * .22f);
        public string WindModeName => wind != null ? wind.ModeName : "Still air";
        public Quaternion Heading => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        public SyntheticGesture Gesture { get => gesture; set => gesture = value; }
        public void SetSyntheticGesture(SyntheticGesture value, bool resetClock)
        {
            gesture = value;
            if (resetClock && input is SyntheticFlightInput synthetic) synthetic.ResetClock();
        }
        private IFlightInput input;
        public BirdFlightController Controller { get; private set; }
        public bool UsesXR { get; private set; }

        private void Start()
        {
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
            var perches = FindObjectsByType<PerchPoint>(FindObjectsInactive.Exclude);
            var perchInfos = new PerchInfo[perches.Length];
            for (int i = 0; i < perches.Length; i++)
            {
                var perchTransform = perches[i].transform;
                var renderer = perches[i].GetComponent<Renderer>();
                float topOffset = renderer != null ? renderer.bounds.max.y - perchTransform.position.y : 0f;
                perchInfos[i] = new PerchInfo(perchTransform.position, perchTransform.rotation, topOffset);
            }
            wind = FindAnyObjectByType<WindField>();
            Controller = new BirdFlightController(input, transform.position, perchInfos, profile, groundHeight: .15f, wind: wind);
            rig = GetComponent<BirdRigDriver>();
            if (UsesXR)
            {
                SubsystemManager.GetSubsystems(xrSubsystems);
                foreach (var subsystem in xrSubsystems) subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
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
            rigInstance.transform.localScale = Vector3.one;
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
            if (Time.deltaTime > 0f) Tick(Mathf.Min(Time.deltaTime, .05f));
        }

        // Explicit runtime step also used by scene regression tests and editor evidence capture.
        public void Tick(float deltaTime)
        {
            if (returningToSelection) return;
            if (input is SyntheticFlightInput synthetic) synthetic.Gesture = gesture;
            // Once per rendered frame so Input System poses and derivative sampling agree.
            // Tests use explicit fixed steps; presentation delta is capped after editor stalls.
            Controller.Step(deltaTime);
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
            var xr = input as XRFlightInput;
            var frame = xr != null ? xr.LastRawFrame : Controller.LastInput;
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
            if (!platformRecenterPending && Time.unscaledTime >= coachUntil)
            {
                if (Controller.State.Phase == FlightPhase.Paused)
                    CoachStatus = "PAUSED - X TO RESUME\nLEFT MENU: CHANGE CHARACTER\nA: START + CALIBRATE";
                else if (Controller.NearestPerchDistance < 18f)
                {
                    CoachStatus = Controller.State.Speed > profile.FlarePerchMaxCaptureSpeed
                        ? "LANDING RING AHEAD\nFLARE: LEFT TRIGGER + SLOW BELOW 10 m/s"
                        : "LANDING RING AHEAD\nGLIDE THROUGH IT + FLARE";
                }
                else if (Controller.WindVelocity.y > 2f)
                    CoachStatus = "RISING AIR: SPREAD WINGS\nGENTLE BANK + CIRCLE TO STAY IN CORE";
                else if (Controller.WindVelocity.y < -2f)
                    CoachStatus = "DESCENDING DRAFT\nLEAVE THE RED STREAM";
                else CoachStatus = null;
            }
            var presentationFrame = xr != null && !calibration.Captured ? FlightInputFrame.Neutral : frame;
            if (rig != null) rig.Present(presentationFrame, calibration, Heading, deltaTime);
        }

        // A is the explicit recovery action: restart flight and capture the held neutral.
        public bool RestartAndCalibrate(FlightInputFrame frame)
        {
            Controller.Reset();
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
            viewMode = FlightViewMode.FirstPerson;
            platformRecenterPending = false;
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
            if (!calibration.CaptureComfortableGlide(frame)) return false;
            Controller.Calibrate(frame);
            if (input is XRFlightInput xr) { xr.WingsEnabled = true; xr.ResetDerivatives(); }
            platformRecenterPending = false;
            CalibrationStatus = $"Calibrated: {calibration.HumanSpanMeters:F2} m span, {calibration.MotionScale:F2} wing scale";
            CoachStatus = message;
            coachUntil = Time.unscaledTime + 4f;
            return true;
        }

        // OpenXR exposes an origin-change notification, not the reserved Meta button.
        // Wait for fresh, stable tracking after that event before accepting a neutral pose.
        public void NotifyTrackingOriginUpdated()
        {
            if (!UsesXR || Controller == null) return;
            platformRecenterPending = true;
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

        public void ReturnToCharacterSelect()
        {
            if (returningToSelection) return;
            returningToSelection = true;
            Time.timeScale = 1f;
            CharacterSelection.Chosen = null;
            SceneManager.LoadScene("CharacterSelect");
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
