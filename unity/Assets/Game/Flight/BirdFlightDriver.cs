using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VoarVR.Input;

namespace VoarVR.Flight
{
    public enum FlightInputMode { Auto, XR, Gamepad, Synthetic }

    public sealed class BirdFlightDriver : MonoBehaviour
    {
        [SerializeField] private FlightInputMode inputMode = FlightInputMode.Auto;
        [SerializeField] private SyntheticGesture gesture = SyntheticGesture.Glide;
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
            Controller = new BirdFlightController(input, transform.position);
        }

        private void Update()
        {
            if (input is SyntheticFlightInput synthetic) synthetic.Gesture = gesture;
            // Once per rendered frame so Input System poses and derivative sampling agree.
            // Tests use explicit fixed steps; presentation delta is capped after editor stalls.
            if (Time.deltaTime <= 0f) return;
            Controller.Step(Mathf.Min(Time.deltaTime, 0.05f));
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
        }

        private void OnDestroy() => (input as IDisposable)?.Dispose();
    }
}
