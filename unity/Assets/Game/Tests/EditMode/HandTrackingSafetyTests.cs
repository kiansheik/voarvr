using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.Telemetry;

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

        [TestCase(.35f)]
        [TestCase(.45f)]
        [TestCase(.55f)]
        public void CompactCalibrationAcceptsBentForwardArmsAndPreservesPhysicalVelocity(float halfSpan)
        {
            var calibration = new BirdTrackingCalibration { CompactHands = true };
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true;
            frame.HeadPosition = Vector3.up * 1.5f;
            frame.LeftWing.Position = new Vector3(-halfSpan, 1.1f, .3f);
            frame.RightWing.Position = new Vector3(halfSpan, 1.1f, .3f);
            frame.LeftWing.Velocity = frame.RightWing.Velocity = Vector3.down;
            Assert.That(calibration.CaptureComfortableGlide(frame), Is.True);
            Assert.That(calibration.HumanSpanMeters, Is.EqualTo(halfSpan * 2).Within(.00001));
            Assert.That(calibration.WingTarget(frame.LeftWing, true).x,
                Is.EqualTo(-calibration.BirdHalfSpanMeters).Within(.00001));
            Assert.That(frame.LeftWing.Velocity, Is.EqualTo(Vector3.down),
                "Compact mapping scales the pose envelope, not measured velocity.");
        }

        [Test]
        public void CompactCalibrationRejectsEstimatedPoseAndKeepsControllerReach()
        {
            var calibration = new BirdTrackingCalibration { CompactHands = true };
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true;
            frame.LeftWing.Position = new Vector3(-.45f, -.4f, .25f);
            frame.RightWing.Position = new Vector3(.45f, -.4f, .25f);
            frame.LeftWing.MotionEstimated = true;
            Assert.That(calibration.CaptureComfortableGlide(frame), Is.False);
            frame.LeftWing.MotionEstimated = false;
            frame.LeftWing.Position = new Vector3(-.7f, -.4f, .25f);
            frame.RightWing.Position = new Vector3(.7f, -.4f, .25f);
            Assert.That(calibration.CaptureComfortableGlide(frame), Is.False);
            calibration.CompactHands = false;
            Assert.That(calibration.CaptureComfortableGlide(frame), Is.True,
                "The original controller calibration still accepts a larger spread.");
        }

        [Test]
        public void EstimatedEffortNeitherEarnsMovementNorBridgesAWingbeat()
        {
            var effort = new FlightEffort();
            var frame = FlightInputFrame.Neutral;
            frame.LeftWing.Velocity = frame.RightWing.Velocity = Vector3.up;
            effort.Step(frame, .05f, true);
            frame.LeftWing.MotionEstimated = true;
            frame.LeftWing.Velocity = frame.RightWing.Velocity = Vector3.down * 8;
            effort.Step(frame, .05f, true);
            Assert.That(effort.HandTravelMeters, Is.EqualTo(.05f).Within(.00001));
            Assert.That(effort.ActiveSeconds, Is.EqualTo(.05f).Within(.00001));
            Assert.That(effort.RestSeconds, Is.Zero);
            frame.LeftWing.MotionEstimated = false;
            effort.Step(frame, .05f, true);
            Assert.That(effort.Strokes, Is.Zero, "A source gap clears the upstroke arm.");
            frame.LeftWing.Velocity = frame.RightWing.Velocity = Vector3.up;
            effort.Step(frame, .05f, true);
            frame.LeftWing.Velocity = frame.RightWing.Velocity = Vector3.down;
            effort.Step(frame, .05f, true);
            Assert.That(effort.Strokes, Is.EqualTo(1));
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

        [TestCase(.27f)]
        [TestCase(.62f)]
        public void CompactCalibrationAcceptsTheRecordedNaturalFlyingPosture(float halfSpan)
        {
            // Controller flights rested hands about 0.5 m below the head, in the shoulder plane.
            var calibration = new BirdTrackingCalibration { CompactHands = true };
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true; frame.HeadPosition = Vector3.up * 1.5f;
            frame.LeftWing.Position = new Vector3(-halfSpan, 1f, .08f);
            frame.RightWing.Position = new Vector3(halfSpan, 1f, .08f);
            Assert.That(calibration.CaptureComfortableGlide(frame), Is.True);
        }

        [Test]
        public void HandsSpreadingPastCalibrationIsNotAHiddenBrake()
        {
            float Brake(bool spanFlare, float spread)
            {
                var input = new Input(); var frame = FlightInputFrame.Neutral;
                frame.LeftWing.Position = new Vector3(frame.LeftWing.Position.x * spread, frame.LeftWing.Position.y, frame.LeftWing.Position.z);
                frame.RightWing.Position = new Vector3(frame.RightWing.Position.x * spread, frame.RightWing.Position.y, frame.RightWing.Position.z);
                input.Frame = frame;
                var controller = new BirdFlightController(input, Vector3.up * 20f) { SpanFlareEnabled = spanFlare };
                controller.Step(1f / 90f);
                return controller.LandingBrake;
            }
            Assert.That(Brake(true, 1.3f), Is.GreaterThan(.5f), "Controllers keep span flare");
            Assert.That(Brake(false, 1.3f), Is.Zero, "Hands in a natural wide spread keep flying");
            var flare = new Input(); var f = FlightInputFrame.Neutral; f.Flare = 1; flare.Frame = f;
            var explicitFlare = new BirdFlightController(flare, Vector3.up * 20f) { SpanFlareEnabled = false };
            explicitFlare.Step(1f / 90f);
            Assert.That(explicitFlare.LandingBrake, Is.EqualTo(1f), "The backstroke flare still brakes");
        }
    }
}
