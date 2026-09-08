using System;
using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Flight
{
    public enum FlightPhase { Gliding, Flapping, Diving, Flaring, Paused }

    public struct BirdState
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public FlightPhase Phase;
        public float Speed => Velocity.magnitude;
    }

    // Deliberately kinematic M0 motion. No realistic lift, drag, collisions or perching.
    public sealed class BirdFlightController
    {
        private readonly IFlightInput input;
        private readonly Vector3 spawn;
        private bool paused;
        public BirdState State { get; private set; }
        public FlightInputFrame LastInput { get; private set; }
        public string InputMode => input.Mode;

        public BirdFlightController(IFlightInput input, Vector3 spawn)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.spawn = spawn;
            Reset();
        }

        public void Reset()
        {
            paused = false;
            LastInput = FlightInputFrame.Neutral;
            State = new BirdState { Position = spawn, Rotation = Quaternion.identity, Phase = FlightPhase.Gliding };
        }

        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            LastInput = input.Sample(deltaTime);
            if (LastInput.ResetPressed) { Reset(); return; }
            if (LastInput.PausePressed) paused = !paused;
            var next = State;
            if (paused)
            {
                next.Velocity = Vector3.zero;
                next.Phase = FlightPhase.Paused;
                State = next;
                return;
            }
            float stroke = Mathf.Clamp(-0.5f * (LastInput.LeftWing.Velocity.y + LastInput.RightWing.Velocity.y), 0f, 3f);
            float tuck = Mathf.Clamp01(LastInput.Tuck);
            float flare = Mathf.Clamp01(LastInput.Flare);
            next.Rotation *= Quaternion.Euler(0f, Mathf.Clamp(LastInput.Bank, -1f, 1f) * 40f * deltaTime, 0f);
            next.Velocity = next.Rotation * Vector3.forward * (3f + tuck * 2f - flare * 2f)
                + Vector3.up * (stroke - tuck * 2f + flare);
            next.Position += next.Velocity * deltaTime;
            next.Phase = tuck > 0.1f ? FlightPhase.Diving : flare > 0.1f ? FlightPhase.Flaring
                : stroke > 0.1f ? FlightPhase.Flapping : FlightPhase.Gliding;
            State = next;
        }
    }
}
