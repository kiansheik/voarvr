using UnityEngine;

namespace VoarVR.Input
{
    public enum SyntheticGesture { Glide, Flap, BankLeft, BankRight, Dive, Flare }

    // Explicit time progression: no wall clock, random numbers, scene or hardware access.
    public sealed class SyntheticFlightInput : IFlightInput
    {
        private float elapsed;
        public SyntheticGesture Gesture { get; set; }
        public string Mode => "Synthetic / " + Gesture;

        public SyntheticFlightInput(SyntheticGesture gesture = SyntheticGesture.Glide)
        {
            Gesture = gesture;
        }

        public FlightInputFrame Sample(float deltaTime)
        {
            elapsed += Mathf.Max(0f, deltaTime);
            var frame = FlightInputFrame.Neutral;
            switch (Gesture)
            {
                case SyntheticGesture.Flap:
                    float phase = elapsed * Mathf.PI * 2f;
                    float height = 0.3f * Mathf.Sin(phase);
                    float velocity = 0.3f * Mathf.PI * 2f * Mathf.Cos(phase);
                    frame.LeftWing.Position.y = frame.RightWing.Position.y = height;
                    frame.LeftWing.Velocity.y = frame.RightWing.Velocity.y = velocity;
                    break;
                case SyntheticGesture.BankLeft: frame.Bank = -1f; break;
                case SyntheticGesture.BankRight: frame.Bank = 1f; break;
                case SyntheticGesture.Dive: frame.Tuck = 1f; break;
                case SyntheticGesture.Flare: frame.Flare = 1f; break;
            }
            return frame;
        }
    }
}
