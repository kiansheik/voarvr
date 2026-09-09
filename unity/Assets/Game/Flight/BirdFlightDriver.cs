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
        [SerializeField] private Transform leftWingVisual;
        [SerializeField] private Transform rightWingVisual;
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
            var perches = FindObjectsByType<PerchPoint>(FindObjectsInactive.Exclude);
            var perchInfos = new PerchInfo[perches.Length];
            for (int i = 0; i < perches.Length; i++)
            {
                var perchTransform = perches[i].transform;
                var renderer = perches[i].GetComponent<Renderer>();
                float topOffset = renderer != null ? renderer.bounds.max.y - perchTransform.position.y : 0f;
                perchInfos[i] = new PerchInfo(perchTransform.position, perchTransform.rotation, topOffset);
            }
            Controller = new BirdFlightController(input, transform.position, perchInfos);
        }

        private void Update()
        {
            if (input is SyntheticFlightInput synthetic) synthetic.Gesture = gesture;
            // Once per rendered frame so Input System poses and derivative sampling agree.
            // Tests use explicit fixed steps; presentation delta is capped after editor stalls.
            if (Time.deltaTime <= 0f) return;
            Controller.Step(Mathf.Min(Time.deltaTime, 0.05f));
            transform.SetPositionAndRotation(Controller.State.Position, Controller.State.Rotation);
            ApplyWingVisuals();
        }

        private void ApplyWingVisuals()
        {
            // Wing pose is tracking-local, the same space FlightCamera uses for the head,
            // so it maps directly onto each wing's local transform under the bird root.
            var frame = Controller.LastInput;
            if (leftWingVisual != null)
                leftWingVisual.SetLocalPositionAndRotation(frame.LeftWing.Position, frame.LeftWing.Orientation);
            if (rightWingVisual != null)
                rightWingVisual.SetLocalPositionAndRotation(frame.RightWing.Position, frame.RightWing.Orientation);
        }

        private void OnDestroy() => (input as IDisposable)?.Dispose();
    }
}
