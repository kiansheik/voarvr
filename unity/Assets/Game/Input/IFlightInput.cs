namespace VoarVR.Input
{
    public interface IFlightInput
    {
        string Mode { get; }
        // Called once per simulation tick. Positions/velocities are in tracking-local meters.
        FlightInputFrame Sample(float deltaTime);
    }
}
