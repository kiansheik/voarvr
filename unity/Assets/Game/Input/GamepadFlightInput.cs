using UnityEngine;
using UnityEngine.InputSystem;

namespace VoarVR.Input
{
    public sealed class GamepadFlightInput : IFlightInput
    {
        private bool resetHeld;
        private bool pauseHeld;
        public string Mode => Gamepad.current == null ? "Gamepad (disconnected)" : "Gamepad";

        public FlightInputFrame Sample(float deltaTime)
        {
            var frame = FlightInputFrame.Neutral;
            var pad = Gamepad.current;
            if (pad == null)
            {
                resetHeld = pauseHeld = false;
                return frame;
            }
            frame.Bank = pad.leftStick.x.ReadValue();
            frame.Tuck = pad.rightTrigger.ReadValue();
            frame.Flare = pad.leftTrigger.ReadValue();
            // Semantic stand-in for downward wing motion, not physical gamepad velocity.
            float stroke = pad.buttonSouth.isPressed ? -2f : 0f;
            frame.LeftWing.Velocity.y = frame.RightWing.Velocity.y = stroke;
            frame.ResetPressed = pad.selectButton.isPressed && !resetHeld;
            frame.PausePressed = pad.startButton.isPressed && !pauseHeld;
            resetHeld = pad.selectButton.isPressed;
            pauseHeld = pad.startButton.isPressed;
            return frame;
        }
    }
}
