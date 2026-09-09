using UnityEngine;

namespace VoarVR.Input
{
    public enum SyntheticGesture { Glide, Flap, BankLeft, BankRight, Dive, Flare, TrackingLoss, StallRecovery }

    // Explicit time progression: no wall clock, random numbers, scene or hardware access.
    public sealed class SyntheticFlightInput : IFlightInput
    {
        private float elapsed;
        public float FlapAmplitude = .3f;
        public SyntheticGesture Gesture { get; set; }
        public string Mode => "Synthetic / " + Gesture;

        public SyntheticFlightInput(SyntheticGesture gesture = SyntheticGesture.Glide)
        {
            Gesture = gesture;
        }

        public void ResetClock() => elapsed = 0f;

        public FlightInputFrame Sample(float deltaTime)
        {
            elapsed += Mathf.Max(0f, deltaTime);
            var frame = FlightInputFrame.Neutral;
            switch (Gesture)
            {
                case SyntheticGesture.Flap:
                    float phase = elapsed * Mathf.PI * 2f;
                    float height = FlapAmplitude * Mathf.Sin(phase);
                    float velocity = FlapAmplitude * Mathf.PI * 2f * Mathf.Cos(phase);
                    frame.LeftWing.Position.y = frame.RightWing.Position.y = height;
                    frame.LeftWing.Velocity.y = frame.RightWing.Velocity.y = velocity;
                    break;
                case SyntheticGesture.BankLeft:
                    frame.LeftWing.Position.y=-.3f; frame.RightWing.Position.y=.3f; break;
                case SyntheticGesture.BankRight:
                    frame.LeftWing.Position.y=.3f; frame.RightWing.Position.y=-.3f; break;
                case SyntheticGesture.Dive:
                    frame.Tuck = 1f;
                    frame.LeftWing.Position = new Vector3(-.20f,-.05f,-.15f);
                    frame.RightWing.Position = new Vector3(.20f,-.05f,-.15f); break;
                case SyntheticGesture.Flare:
                    frame.Flare = 1f;
                    frame.LeftWing.Position.z=frame.RightWing.Position.z=.18f;
                    frame.LeftWing.Orientation=frame.RightWing.Orientation=Quaternion.Euler(-35f,0f,0f); break;
                case SyntheticGesture.TrackingLoss:
                    frame.LeftWing.Tracked=frame.RightWing.Tracked=(elapsed%4f)<2f; break;
                case SyntheticGesture.StallRecovery:
                    frame.Flare=elapsed<2f ? 1f : 0f;
                    if(elapsed>=2f) frame.LeftWing.Velocity=frame.RightWing.Velocity=Vector3.down*2f;
                    break;
            }
            return frame;
        }
    }
}
