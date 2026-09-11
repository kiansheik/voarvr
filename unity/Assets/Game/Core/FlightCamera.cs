using UnityEngine;
using UnityEngine.InputSystem;
using VoarVR.Flight;

namespace VoarVR.Core
{
    public sealed class FlightCamera : MonoBehaviour
    {
        [SerializeField] private BirdFlightDriver bird;
        [SerializeField] private bool firstPerson = true;
        private InputAction position, rotation, tracked;
        private Vector3 lastValidPosition;
        private Quaternion lastValidRotation = Quaternion.identity;
        private TextMesh calibrationPrompt;
        private string displayedPrompt;
        public bool FirstPerson { get => firstPerson; set => firstPerson=value; }
        private void OnEnable()
        {
            position = new InputAction("HeadPosition", binding: "<XRHMD>/centerEyePosition");
            rotation = new InputAction("HeadRotation", binding: "<XRHMD>/centerEyeRotation");
            tracked = new InputAction("HeadTracked", binding: "<XRHMD>/isTracked");
            position.Enable(); rotation.Enable(); tracked.Enable();
            Application.onBeforeRender += ApplyPose;
        }
        private void LateUpdate() => ApplyPose();
        private void ApplyPose()
        {
            if (bird == null) return;
            var rig=bird.GetComponent<BirdRigDriver>();
            // Keep tracked head samples live in both views. The platform can toggle views
            // while the player continues to look and move around the chase camera.
            if (bird.UsesXR && tracked.ReadValue<float>() > 0f)
            {
                var candidateRotation = rotation.ReadValue<Quaternion>();
                if (candidateRotation != default(Quaternion)) lastValidRotation = candidateRotation;
                lastValidPosition = position.ReadValue<Vector3>();
            }
            var cameraTrackingHeading = bird.Calibration.Heading * Quaternion.Euler(0f, bird.PhysicalYawOffsetDeg, 0f);
            bool useFirstPerson = bird.ViewMode == FlightViewMode.FirstPerson && (bird.UsesXR || firstPerson);
            if (rig != null) rig.SetFirstPersonVisibility(useFirstPerson);
            if (useFirstPerson)
            {
                var headRotation = bird.UsesXR ? lastValidRotation : Quaternion.identity;
                var offset = bird.UsesXR && bird.Calibration.HeadCaptured
                    ? Quaternion.Inverse(cameraTrackingHeading) * (lastValidPosition - bird.Calibration.HeadOrigin) : Vector3.zero;
                bool advanced=bird.Controller.ControlMode==FlightControlMode.Acrobatic;
                var basis=FirstPersonBasis(bird.Controller.ControlMode,bird.transform.rotation,bird.Heading,VoarVR.UI.FlightPreferences.FirstPersonStabilized);
                transform.SetPositionAndRotation(bird.transform.position + basis * ((advanced?bird.ImmersiveEyeAnchor:bird.CameraEyeAnchor) + offset),
                    basis * (advanced?Quaternion.identity:Quaternion.Euler(6f,0f,0f))
                    * (bird.UsesXR
                        ? Quaternion.Inverse(bird.Calibration.NeutralLookPitch) * Quaternion.Inverse(cameraTrackingHeading)
                        : Quaternion.identity) * headRotation);
                UpdateCalibrationPrompt();
            }
            else
            {
                float extraWingReach = Mathf.Max(0f, bird.CharacterRestHalfSpan - .56f);
                float chaseDistance = 2.35f + extraWingReach * 1.7f;
                float chaseHeight = .95f + extraWingReach * .6f;
                var physicalOffset = bird.UsesXR && bird.Calibration.HeadCaptured
                    ? Quaternion.Inverse(cameraTrackingHeading) * (lastValidPosition - bird.Calibration.HeadOrigin) : Vector3.zero;
                var desired = bird.transform.position + bird.Heading
                    * (new Vector3(0f, chaseHeight, -chaseDistance) + physicalOffset);
                var origin = bird.transform.position + bird.Heading * (Vector3.up * .3f + physicalOffset);
                transform.position = ConstrainChasePosition(origin, desired, bird.GetComponent<VoarVR.World.UnityFlightEnvironment>());
                var baseRotation = ChaseBaseRotation(bird.Heading, chaseHeight, chaseDistance, extraWingReach);
                var headDelta = bird.UsesXR
                    ? Quaternion.Inverse(bird.Calibration.NeutralLookPitch) * Quaternion.Inverse(cameraTrackingHeading) * lastValidRotation
                    : Quaternion.identity;
                transform.rotation = baseRotation * headDelta;
                UpdateCalibrationPrompt();
            }
        }

        private void UpdateCalibrationPrompt()
        {
            if (!bird.UsesXR) return;
            if (calibrationPrompt == null)
            {
                var prompt = new GameObject("CalibrationPrompt");
                prompt.transform.SetParent(transform, false);
                prompt.transform.localPosition = new Vector3(0f, -.12f, .7f);
                calibrationPrompt = prompt.AddComponent<TextMesh>();
                displayedPrompt = null;
                calibrationPrompt.anchor = TextAnchor.MiddleCenter;
                calibrationPrompt.alignment = TextAlignment.Center;
                calibrationPrompt.fontSize = 48;
                calibrationPrompt.characterSize = .008f;
                calibrationPrompt.color = new Color(1f, .82f, .25f);
            }
            calibrationPrompt.gameObject.SetActive(!bird.FlightMenuVisible && (!bird.Calibration.Captured || (bird.ShowFlightText && !string.IsNullOrEmpty(bird.CoachStatus))));
            if (!calibrationPrompt.gameObject.activeSelf) return;
            string text = ResolvePrompt(bird.Calibration.Captured, bird.CalibrationStatus, bird.CoachStatus);
            if (displayedPrompt == text) return;
            displayedPrompt = text;
            calibrationPrompt.text = text;
        }

        public static Quaternion ChaseBaseRotation(Quaternion heading, float height, float distance, float extraReach) =>
            heading * Quaternion.LookRotation(new Vector3(0f, .12f + extraReach * .15f - height, .25f + distance), Vector3.up);

        public static Quaternion FirstPersonBasis(FlightControlMode mode,Quaternion body,Quaternion heading, bool stabilized=false) =>
            mode==FlightControlMode.Acrobatic && !stabilized?body:heading;

        public static Vector3 ConstrainChasePosition(Vector3 origin, Vector3 desired, IFlightEnvironment environment)
        {
            if (environment == null || !environment.Sweep(origin, desired, .14f, out var hit)) return desired;
            return hit.Position;
        }

        public static string ResolvePrompt(bool calibrated, string calibrationStatus, string coachStatus) =>
            calibrated ? coachStatus ?? ""
                : (calibrationStatus != null && calibrationStatus.StartsWith("Calibration rejected")
                    ? "POSE NOT ACCEPTED: LEVEL YOUR WINGS\nPRESS A: START + CALIBRATE\nLEFT MENU: CHANGE CHARACTER"
                    : (calibrationStatus != null && calibrationStatus.StartsWith("Journey resumed")
                        ? "JOURNEY PAUSED · PROGRESS KEPT\nRIGHT TRIGGER: FLIGHT MENU\nCHOOSE RECALIBRATE HERE"
                        : calibrationStatus != null && calibrationStatus.StartsWith("Platform recentered")
                        ? "HOLD COMFORTABLE SPREAD STILL\nRECENTER CALIBRATES HERE"
                        : "HOLD A LEVEL COMFORTABLE T POSE\nPRESS A: START + CALIBRATE\nLEFT MENU: CHANGE CHARACTER"));

        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyPose;
            position?.Dispose(); rotation?.Dispose(); tracked?.Dispose();
            if (calibrationPrompt != null) Destroy(calibrationPrompt.gameObject);
        }
    }
}
