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
        private readonly InputAction leftTracked, rightTracked, headRotation, headPosition, headTracked, tuck, flare, recalibrate, pause, viewToggle, windMode, characterSelect, hudToggle, groundMove, leftGrip, rightGrip, markerClick;
        private WingInput previousLeft, previousRight;
        private readonly TrackedBodyFrame body = new TrackedBodyFrame();
        private readonly ControlModeGesture modeGesture=new ControlModeGesture();
        private bool recalibrateHeld, pauseHeld, viewHeld, windHeld, menuHeld, hudHeld, markerHeld;
        public FlightInputFrame LastDeviceFrame { get; private set; }
        public FlightInputFrame LastRawFrame { get; private set; }
        public bool WingsEnabled { get; set; }
        public bool LastWingsEnabled { get; private set; }
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
            recalibrate = Action("Recalibrate", "<XRController>{RightHand}/primaryButton");
            pause = Action("Pause", "<XRController>{LeftHand}/primaryButton");
            viewToggle = Action("ViewToggle", "<XRController>{RightHand}/secondaryButton");
            windMode = Action("WindMode", "<XRController>{LeftHand}/secondaryButton");
            characterSelect = Action("CharacterSelect", "<XRController>{LeftHand}/menuButton");
            hudToggle = Action("HudToggle", "<XRController>{RightHand}/{Primary2DAxisClick}");
            groundMove = Action("GroundMove", "<XRController>{LeftHand}/primary2DAxis");
            leftGrip = Action("LeftGrip", "<XRController>{LeftHand}/grip");
            rightGrip = Action("RightGrip", "<XRController>{RightHand}/grip");
            markerClick = Action("MarkerClick", "<XRController>{LeftHand}/{Primary2DAxisClick}");
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
            LastWingsEnabled=WingsEnabled;
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
            bool recalibrateNow = recalibrate.ReadValue<float>() > 0.5f;
            bool pauseNow = pause.ReadValue<float>() > 0.5f;
            bool viewNow = viewToggle.ReadValue<float>() > 0.5f;
            bool windNow = windMode.ReadValue<float>() > 0.5f;
            frame.RecalibratePressed = recalibrateNow && !recalibrateHeld;
            frame.ResetPressed = frame.RecalibratePressed;
            bool menuNow = characterSelect.ReadValue<float>() > .5f;
            frame.CharacterSelectPressed = menuNow && !menuHeld;
            menuHeld = menuNow;
            frame.PausePressed = pauseNow && !pauseHeld;
            frame.ViewTogglePressed = viewNow && !viewHeld;
            frame.WindModePressed = windNow && !windHeld;
            recalibrateHeld = recalibrateNow;
            pauseHeld = pauseNow;
            viewHeld = viewNow;
            windHeld = windNow;
            if (frame.RecalibratePressed)
            {
                // Restart captures this pose; avoid carrying an old derivative into flight.
                previousLeft = previousRight = default;
            }
            bool hudNow = hudToggle.ReadValue<float>() > .5f;
            frame.HudTogglePressed = hudNow && !hudHeld; hudHeld = hudNow;
            frame.GroundMove = Vector2.ClampMagnitude(groundMove.ReadValue<Vector2>(), 1f);
            bool leftGripNow = leftGrip.ReadValue<float>() > .75f;
            bool rightGripNow = rightGrip.ReadValue<float>() > .75f;
            bool clickNow = markerClick.ReadValue<float>() > .5f;
            bool markNow = leftGripNow && rightGripNow && clickNow;
            frame.ControlModePressed=modeGesture.Sample(clickNow,leftGripNow || rightGripNow,deltaTime) && WingsEnabled;
            frame.MarkerPressed = markNow && !markerHeld; markerHeld = markNow;
            frame.ButtonsHeld = (recalibrateNow?1u:0u) | (viewNow?2u:0u) | (pauseNow?4u:0u)
                | (windNow?8u:0u) | (menuNow?16u:0u) | (hudNow?32u:0u)
                | (clickNow?64u:0u) | (leftGripNow?128u:0u) | (rightGripNow?256u:0u);
            var deviceFrame = frame;
            body.Sample(ref frame, deltaTime);
            deviceFrame.BodyOrientation=frame.BodyOrientation; deviceFrame.BodyTracked=frame.BodyTracked;
            deviceFrame.LeftWing.Velocity=frame.LeftWing.Velocity; deviceFrame.RightWing.Velocity=frame.RightWing.Velocity;
            LastDeviceFrame=deviceFrame;
            LastRawFrame = frame;
            if (!WingsEnabled)
            {
                // Keep the bird stable until the player deliberately establishes the
                // human-to-duck frame. Reset/pause and valid HMD data still pass through.
                frame.LeftWing = FlightInputFrame.Neutral.LeftWing;
                frame.RightWing = FlightInputFrame.Neutral.RightWing;
                frame.Bank = frame.Tuck = frame.Flare = 0f;
                frame.BodyTracked = false;
            }
            return frame;
        }

        public void ResetDerivatives()
        {
            body.ResetDerivatives();
            previousLeft = previousRight = default;
        }

        public void ResetTrackingOrigin() { body.Reset(); ResetDerivatives(); }

        public void Dispose() => actions.Dispose();
    }
}
