using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class HandTrackingSafetyTests
    {
        private sealed class Input : IFlightInput
        {
            public FlightInputFrame Frame = FlightInputFrame.Neutral;
            public string Mode => "Hand tracking safety fixture";
            public FlightInputFrame Sample(float deltaTime) => Frame;
        }

        [Test]
        public void EstimatedWingMotionCannotInjectActiveStrokeEnergyButPoseRemainsTracked()
        {
            var estimatedInput = new Input();
            var estimated = FlightInputFrame.Neutral;
            estimated.LeftWing.Velocity = Vector3.down * 2.5f;
            estimated.RightWing.Velocity = Vector3.down * 2.5f;
            estimated.LeftWing.MotionEstimated = true;
            estimated.RightWing.MotionEstimated = true;
            estimatedInput.Frame = estimated;

            var estimatedController = new BirdFlightController(estimatedInput, Vector3.up * 20f);
            estimatedController.Step(1f / 90f);

            Assert.That(estimatedController.LastInput.LeftWing.Tracked, Is.True,
                "An inferred pose may remain available for continuity and steering.");
            Assert.That(estimatedController.LastInput.RightWing.Tracked, Is.True);
            Assert.That(estimatedController.StrokeForce.sqrMagnitude, Is.LessThan(1e-6f),
                "Predicted or inferred hand motion must never manufacture active flap work.");

            var directInput = new Input();
            var direct = FlightInputFrame.Neutral;
            direct.LeftWing.Velocity = Vector3.down * 2.5f;
            direct.RightWing.Velocity = Vector3.down * 2.5f;
            directInput.Frame = direct;

            var directController = new BirdFlightController(directInput, Vector3.up * 20f);
            directController.Step(1f / 90f);

            Assert.That(directController.StrokeForce.sqrMagnitude, Is.GreaterThan(1f),
                "The same measured downstroke should still create active force when motion is direct.");
        }
    }
}
