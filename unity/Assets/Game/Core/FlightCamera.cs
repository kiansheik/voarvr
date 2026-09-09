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
        private string displayedCalibrationStatus;
        private string displayedCoachStatus;
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
            bool useFirstPerson = bird.ViewMode == FlightViewMode.FirstPerson && (bird.UsesXR || firstPerson);
            if (rig != null) rig.SetFirstPersonVisibility(useFirstPerson);
            if (useFirstPerson)
            {
                var headRotation = bird.UsesXR ? lastValidRotation : Quaternion.identity;
                var offset = bird.UsesXR && bird.Calibration.HeadCaptured
                    ? bird.Calibration.HeadOffset(lastValidPosition) : Vector3.zero;
                // Yaw-only basis: simulation bank/pitch never rotates the headset or its room offset.
                transform.SetPositionAndRotation(bird.transform.position + bird.Heading * (BirdTrackingCalibration.EyeAnchor + offset),
                    bird.Heading * Quaternion.Euler(6f,0f,0f)
                    * (bird.UsesXR ? Quaternion.Inverse(bird.Calibration.Heading) : Quaternion.identity) * headRotation);
                UpdateCalibrationPrompt();
            }
            else
            {
                var physicalOffset = bird.UsesXR && bird.Calibration.HeadCaptured
                    ? bird.Calibration.HeadOffset(lastValidPosition) : Vector3.zero;
                transform.position = bird.transform.position + bird.Heading * (new Vector3(0f, .95f, -2.35f) + physicalOffset);
                var baseRotation = Quaternion.LookRotation(
                    bird.transform.position + bird.Heading * new Vector3(0f, .12f, .25f) - transform.position,
                    Vector3.up);
                var headDelta = bird.UsesXR
                    ? Quaternion.Inverse(bird.Calibration.Heading) * lastValidRotation : Quaternion.identity;
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
                calibrationPrompt.anchor = TextAnchor.MiddleCenter;
                calibrationPrompt.alignment = TextAlignment.Center;
                calibrationPrompt.fontSize = 48;
                calibrationPrompt.characterSize = .008f;
                calibrationPrompt.color = new Color(1f, .82f, .25f);
            }
            calibrationPrompt.gameObject.SetActive(!bird.Calibration.Captured || !string.IsNullOrEmpty(bird.CoachStatus));
            if (!calibrationPrompt.gameObject.activeSelf) return;
            if (!bird.Calibration.Captured)
            {
                if (displayedCalibrationStatus == bird.CalibrationStatus) return;
                displayedCalibrationStatus = bird.CalibrationStatus;
                calibrationPrompt.text = bird.CalibrationStatus.StartsWith("Platform recentered")
                    ? "HOLD COMFORTABLE T POSE\nLOOK FORWARD"
                    : bird.CalibrationStatus.StartsWith("Calibration rejected")
                        ? "TRACK HEAD + HANDS\nSPREAD ARMS 0.7-2.2 m\nPRESS RIGHT PRIMARY"
                        : "SPREAD ARMS\nPRESS RIGHT PRIMARY";
            }
            else if (displayedCoachStatus != bird.CoachStatus)
            {
                displayedCoachStatus = bird.CoachStatus;
                calibrationPrompt.text = bird.CoachStatus;
            }
        }
        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyPose;
            position?.Dispose(); rotation?.Dispose(); tracked?.Dispose();
            if (calibrationPrompt != null) Destroy(calibrationPrompt.gameObject);
        }
    }
}
