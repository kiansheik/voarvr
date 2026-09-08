using UnityEngine;
using UnityEngine.InputSystem;
using VoarVR.Flight;

namespace VoarVR.Core
{
    public sealed class FlightCamera : MonoBehaviour
    {
        [SerializeField] private BirdFlightDriver bird;
        private InputAction position, rotation;

        private void OnEnable()
        {
            position = new InputAction("HeadPosition", binding: "<XRHMD>/centerEyePosition");
            rotation = new InputAction("HeadRotation", binding: "<XRHMD>/centerEyeRotation");
            position.Enable();
            rotation.Enable();
            Application.onBeforeRender += ApplyPose;
        }

        private void LateUpdate() => ApplyPose();
        private void ApplyPose()
        {
            if (bird == null) return;
            if (bird.UsesXR)
            {
                // Bird is the tracking origin. Preserve physical head pose; no artificial roll.
                var headRotation = rotation.ReadValue<Quaternion>();
                if (headRotation == default(Quaternion)) headRotation = Quaternion.identity;
                transform.SetPositionAndRotation(bird.transform.TransformPoint(position.ReadValue<Vector3>()),
                    bird.transform.rotation * headRotation);
            }
            else
            {
                transform.position = bird.transform.TransformPoint(new Vector3(0f, 2f, -6f));
                transform.LookAt(bird.transform.position + Vector3.up * 0.5f);
            }
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyPose;
            position?.Dispose();
            rotation?.Dispose();
        }
    }
}
