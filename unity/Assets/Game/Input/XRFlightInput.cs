using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VoarVR.Input
{
    // Input System actions are required by OpenXR. No Meta SDK or XRI dependency.
    public sealed class XRFlightInput : IFlightInput, IDisposable
    {
        private readonly InputActionMap actions = new InputActionMap("Flight");
        private readonly InputAction leftPosition, rightPosition, leftRotation, rightRotation;
        private readonly InputAction leftTracked, rightTracked, headRotation, headPosition, headTracked, tuck, flare, reset, pause, viewToggle, windMode;
        private WingInput previousLeft, previousRight;
        private bool resetHeld, pauseHeld, viewHeld, windHeld;
        public FlightInputFrame LastRawFrame { get; private set; }
        public bool WingsEnabled { get; set; }
        public string Mode => "XR / OpenXR";

        public XRFlightInput()
        {
            leftPosition = Action("LeftPosition", "<XRController>{LeftHand}/devicePosition");
            rightPosition = Action("RightPosition", "<XRController>{RightHand}/devicePosition");
            leftRotation = Action("LeftRotation", "<XRController>{LeftHand}/deviceRotation");
            rightRotation = Action("RightRotation", "<XRController>{RightHand}/deviceRotation");
            leftTracked = Action("LeftTracked", "<XRController>{LeftHand}/isTracked");
            rightTracked = Action("RightTracked", "<XRController>{RightHand}/isTracked");
            headPosition = Action("HeadPosition", "<XRHMD>/centerEyePosition");
            headTracked = Action("HeadTracked", "<XRHMD>/isTracked");
            headRotation = Action("HeadRotation", "<XRHMD>/centerEyeRotation");
            tuck = Action("Tuck", "<XRController>{RightHand}/trigger");
            flare = Action("Flare", "<XRController>{LeftHand}/trigger");
            reset = Action("Reset", "<XRController>{RightHand}/primaryButton");
            pause = Action("Pause", "<XRController>{LeftHand}/primaryButton");
            viewToggle = Action("ViewToggle", "<XRController>{RightHand}/secondaryButton");
            windMode = Action("WindMode", "<XRController>{LeftHand}/secondaryButton");
            actions.Enable();
        }

        private InputAction Action(string name, string binding) =>
            actions.AddAction(name, InputActionType.Value, binding);

        private static WingInput ReadWing(InputAction position, InputAction rotation,
            InputAction tracked, WingInput previous, float deltaTime)
        {
            bool valid = tracked.ReadValue<float>() > 0f;
            var wing = new WingInput
            {
                Position = valid ? position.ReadValue<Vector3>() : Vector3.zero,
                Orientation = valid ? rotation.ReadValue<Quaternion>() : Quaternion.identity,
                Tracked = valid
            };
            // First sample after tracking loss/recenter must not produce a velocity spike.
            if (valid && previous.Tracked && deltaTime > 0f)
                wing.Velocity = (wing.Position - previous.Position) / deltaTime;
            return wing;
        }

        public FlightInputFrame Sample(float deltaTime)
        {
            var frame = FlightInputFrame.Neutral;
            frame.LeftWing = ReadWing(leftPosition, leftRotation, leftTracked, previousLeft, deltaTime);
            frame.RightWing = ReadWing(rightPosition, rightRotation, rightTracked, previousRight, deltaTime);
            previousLeft = frame.LeftWing;
            previousRight = frame.RightWing;
            var rotation = headRotation.ReadValue<Quaternion>();
            frame.HeadPosition = headPosition.ReadValue<Vector3>();
            frame.HeadTracked = headTracked.ReadValue<float>() > 0f;
            frame.HeadOrientation = rotation == default(Quaternion) ? Quaternion.identity : rotation;
            frame.LookDirection = rotation == default(Quaternion) ? Vector3.forward : rotation * Vector3.forward;
            frame.Bank = frame.LeftWing.Tracked && frame.RightWing.Tracked
                ? Mathf.Clamp(frame.LeftWing.Position.y - frame.RightWing.Position.y, -1f, 1f) : 0f;
            frame.Tuck = tuck.ReadValue<float>();
            frame.Flare = flare.ReadValue<float>();
            bool resetNow = reset.ReadValue<float>() > 0.5f;
            bool pauseNow = pause.ReadValue<float>() > 0.5f;
            bool viewNow = viewToggle.ReadValue<float>() > 0.5f;
            bool windNow = windMode.ReadValue<float>() > 0.5f;
            frame.ResetPressed = resetNow && !resetHeld;
            frame.PausePressed = pauseNow && !pauseHeld;
            frame.ViewTogglePressed = viewNow && !viewHeld;
            frame.WindModePressed = windNow && !windHeld;
            resetHeld = resetNow;
            pauseHeld = pauseNow;
            viewHeld = viewNow;
            windHeld = windNow;
            if (frame.ResetPressed)
            {
                // App-space recenter captures this pose; avoid an asynchronous origin jump.
                previousLeft = previousRight = default;
            }
            LastRawFrame = frame;
            if (!WingsEnabled)
            {
                // Keep the bird stable until the player deliberately establishes the
                // human-to-duck frame. Reset/pause and valid HMD data still pass through.
                frame.LeftWing = FlightInputFrame.Neutral.LeftWing;
                frame.RightWing = FlightInputFrame.Neutral.RightWing;
                frame.Bank = frame.Tuck = frame.Flare = 0f;
            }
            return frame;
        }

        public void ResetDerivatives()
        {
            previousLeft = previousRight = default;
        }

        public void Dispose() => actions.Dispose();
    }
}
