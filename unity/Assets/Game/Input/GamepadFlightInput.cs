using UnityEngine;
using UnityEngine.InputSystem;

namespace VoarVR.Input
{
    public sealed class GamepadFlightInput : IFlightInput
    {
        private readonly ControlModeGesture modeGesture=new ControlModeGesture();
        private bool resetHeld;
        private bool pauseHeld;
        private bool viewHeld;
        private bool windHeld;
        private bool menuHeld;
        private bool hudHeld;
        public string Mode => Gamepad.current == null ? "Gamepad (disconnected)" : "Gamepad";

        public FlightInputFrame Sample(float deltaTime)
        {
            var frame = FlightInputFrame.Neutral;
            var pad = Gamepad.current;
            if (pad == null)
            {
                resetHeld = pauseHeld = viewHeld = windHeld = menuHeld = hudHeld = false;
                return frame;
            }
            frame.ControlModePressed=modeGesture.Sample(pad.leftStickButton.isPressed,false,deltaTime);
            frame.Bank = pad.leftStick.x.ReadValue();
            frame.GroundMove = pad.leftStick.ReadValue();
            frame.HudTogglePressed = pad.rightStickButton.isPressed && !hudHeld;
            hudHeld = pad.rightStickButton.isPressed;
            frame.Tuck = pad.rightTrigger.ReadValue();
            frame.Flare = pad.leftTrigger.ReadValue();
            // Semantic stand-in for downward wing motion, not physical gamepad velocity.
            float stroke = pad.buttonSouth.isPressed ? -2f : 0f;
            frame.LeftWing.Velocity.y = frame.RightWing.Velocity.y = stroke;
            frame.CharacterSelectPressed = pad.buttonEast.isPressed && !menuHeld;
            menuHeld = pad.buttonEast.isPressed;
            frame.ResetPressed = pad.selectButton.isPressed && !resetHeld;
            frame.PausePressed = pad.startButton.isPressed && !pauseHeld;
            frame.ViewTogglePressed = pad.buttonNorth.isPressed && !viewHeld;
            frame.WindModePressed = pad.buttonWest.isPressed && !windHeld;
            resetHeld = pad.selectButton.isPressed;
            pauseHeld = pad.startButton.isPressed;
            viewHeld = pad.buttonNorth.isPressed;
            windHeld = pad.buttonWest.isPressed;
            return frame;
        }
    }
}
