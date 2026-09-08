using UnityEngine;

namespace VoarVR.Input
{
    public struct WingInput
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Orientation;
        public bool Tracked;

        public static WingInput Rest(Vector3 position) => new WingInput
        {
            Position = position, Orientation = Quaternion.identity, Tracked = true
        };
    }

    public struct FlightInputFrame
    {
        public WingInput LeftWing;
        public WingInput RightWing;
        public Vector3 LookDirection;
        public float Bank; // -1 left, +1 right: provisional semantic mapping.
        public float Tuck; // 0..1
        public float Flare; // 0..1
        public bool ResetPressed;
        public bool PausePressed;

        public static FlightInputFrame Neutral => new FlightInputFrame
        {
            LeftWing = WingInput.Rest(new Vector3(-0.6f, 0f, 0f)),
            RightWing = WingInput.Rest(new Vector3(0.6f, 0f, 0f)),
            LookDirection = Vector3.forward
        };
    }
}
