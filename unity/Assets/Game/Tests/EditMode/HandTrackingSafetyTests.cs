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
        public void EstimatedDownstrokeCannotLaunchFromSupportedRest()
        {
            var input=new Input();
            var profile=BirdFlightProfile.Duck();
            var controller=new BirdFlightController(input,Vector3.up*(profile.CollisionRadius+.01f),
                profile:profile,environment:new PlaneFlightEnvironment(0));
            for(int i=0;i<300&&controller.State.Phase!=FlightPhase.Perched;i++)controller.Step(1f/120f);
            Assert.That(controller.State.Phase,Is.EqualTo(FlightPhase.Perched));
            input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*2.5f;
            input.Frame.LeftWing.MotionEstimated=input.Frame.RightWing.MotionEstimated=true;
            for(int i=0;i<30;i++)controller.Step(1f/120f);
            Assert.That(controller.State.Phase,Is.EqualTo(FlightPhase.Perched));
            Assert.That(controller.TakeoffCount,Is.Zero);
            input.Frame.LeftWing.MotionEstimated=input.Frame.RightWing.MotionEstimated=false;
            controller.Step(1f/120f);
            Assert.That(controller.State.Phase,Is.Not.EqualTo(FlightPhase.Perched));
            Assert.That(controller.TakeoffCount,Is.EqualTo(1));
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
