using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.Diagnostics
{
    public sealed class FlightDiagnostics : MonoBehaviour
    {
        [SerializeField] private BirdFlightDriver driver;
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private float speed, verticalSpeed;
        [SerializeField] private string inputMode, flightState;
        [SerializeField] private Vector3 leftWingVelocity, rightWingVelocity;

        private void Awake() { if (driver == null) driver = GetComponent<BirdFlightDriver>(); }
        private void LateUpdate()
        {
            var controller = driver != null ? driver.Controller : null;
            if (controller == null) return;
            speed = controller.State.Speed;
            verticalSpeed = controller.State.Velocity.y;
            inputMode = controller.InputMode;
            flightState = controller.State.Phase.ToString();
            leftWingVelocity = controller.LastInput.LeftWing.Velocity;
            rightWingVelocity = controller.LastInput.RightWing.Velocity;
        }

        private void OnGUI()
        {
            // IMGUI is an Editor/desktop convenience, not a stereo VR HUD.
            if (!showOverlay || driver == null || driver.UsesXR) return;
            GUI.Box(new Rect(12, 12, 370, 145), "VoarVR foundation diagnostics");
            GUI.Label(new Rect(24, 36, 345, 120), $"Mode: {inputMode}\nSpeed: {speed:F2} m/s   Vertical: {verticalSpeed:F2} m/s\nLeft wing: {leftWingVelocity:F2}\nRight wing: {rightWingVelocity:F2}\nState: {flightState}");
        }
    }
}
