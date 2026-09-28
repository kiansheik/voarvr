using UnityEngine;

namespace VoarVR.Input
{
    // Confirming a paused menu must not become a tuck or a walking step on resume.
    // Raw device samples remain available to the menu, calibration and telemetry.
    public sealed class FlightActionGate : IFlightInput
    {
        private readonly IFlightInput source;
        private bool releaseTrigger, releaseStick, releaseTurn;
        public FlightActionGate(IFlightInput source) { this.source = source; }
        public string Mode => source.Mode;
        public void RequireRelease() { releaseTrigger = releaseStick = releaseTurn = true; }
        public FlightInputFrame Sample(float dt)
        {
            var frame = source.Sample(dt);
            if (releaseTrigger)
            {
                releaseTrigger = frame.Tuck > .25f;
                frame.Tuck = 0;
            }
            if (releaseStick)
            {
                releaseStick = frame.GroundMove.magnitude > .25f;
                frame.GroundMove = Vector2.zero;
            }
            if (releaseTurn)
            {
                releaseTurn = Mathf.Abs(frame.GroundTurn) > .25f;
                frame.GroundTurn = 0f;
            }
            return frame;
        }
    }
}
