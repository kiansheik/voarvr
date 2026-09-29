using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class HandCalibrationFlowTests
    {
        private static FlightInputFrame Glide(float yaw = 0f, HandPoseSource left = HandPoseSource.DirectHigh,
            HandPoseSource right = HandPoseSource.DirectHigh, float leftShift = 0f)
        {
            // The recorded natural glide pose: 0.64 m out, 0.15 m below the eyes, in the head plane.
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true; frame.HeadPosition = new Vector3(0, 1.5f, 0);
            frame.HeadOrientation = Quaternion.Euler(0, yaw, 0);
            frame.LeftWing = Hand(new Vector3(-.64f + leftShift, 1.35f, 0), left);
            frame.RightWing = Hand(new Vector3(.64f, 1.35f, 0), right);
            return frame;
        }

        private static WingInput Hand(Vector3 position, HandPoseSource source) => new WingInput
        { Position = position, Orientation = Quaternion.identity, Tracked = true, Source = source,
          MotionEstimated = source != HandPoseSource.DirectHigh };

        private static void Run(HandCalibrationFlow flow, FlightInputFrame frame, FlightInputFrame device, float seconds)
        { for (float t = 0; t < seconds; t += .02f) flow.Sample(frame, device, .02f); }

        [Test]
        public void FullRunNeedsLookAroundAndStartThenCountsDownHandsFree()
        {
            var flow = new HandCalibrationFlow(); flow.Begin(true);
            var glide = Glide();
            Run(flow, glide, glide, 1.1f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.LookLeft), "A steady natural pose advances by itself");
            // Looking left hides the right hand from the cameras; its inferred pose sits 30 cm off.
            var left = Glide(-40f, right: HandPoseSource.Inferred);
            var leftDevice = left; leftDevice.RightWing.Position += new Vector3(-.3f, 0, 0);
            Run(flow, left, leftDevice, .6f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.LookRight));
            var right = Glide(40f, left: HandPoseSource.Inferred);
            var rightDevice = right; rightDevice.LeftWing.Position += new Vector3(.2f, .1f, 0);
            Run(flow, right, rightDevice, .6f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Start), "The pose alone never completes a first calibration");
            Assert.That(flow.RightBiasKnown && flow.LeftBiasKnown, Is.True);
            Assert.That(flow.RightInferredBias.x, Is.EqualTo(.3f).Within(.01f));
            Assert.That(flow.LeftInferredBias.x, Is.EqualTo(-.2f).Within(.01f));
            Assert.That(flow.LeftInferredBias.y, Is.EqualTo(-.1f).Within(.01f));
            Run(flow, glide, glide, 5f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Start), "Waits for START");
            flow.PressStart();
            bool done = false;
            for (float t = 0; t < 3.5f && !done; t += .02f) done = flow.Sample(glide, glide, .02f);
            Assert.That(done, Is.True); Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Done));
        }

        [Test]
        public void CountdownSurvivesBlipsButRestartsWhenThePoseBreaks()
        {
            var flow = new HandCalibrationFlow(); flow.Begin(true);
            var glide = Glide();
            Run(flow, glide, glide, 1.1f);
            Run(flow, Glide(-40f), Glide(-40f), .6f); Run(flow, Glide(40f), Glide(40f), .6f);
            flow.PressStart();
            Run(flow, glide, glide, 2f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Countdown));
            var lost = glide; lost.RightWing.Tracked = false;
            Run(flow, lost, lost, .1f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Countdown), "A short tracking blip keeps the count");
            Run(flow, lost, lost, .4f);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Return), "Leaving the pose restarts the count");
            // Moving hands are not a pose.
            for (int i = 0; i < 40; i++) { var moving = Glide(leftShift: (i % 2) * .05f); flow.Sample(moving, moving, .02f); }
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Return));
        }

        [Test]
        public void RedoSkipsLookAroundOnceContextIsKnownAndInferredPosesCount()
        {
            var flow = new HandCalibrationFlow(); flow.Begin(false);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Pose), "No context yet: a redo still teaches it");
            var glide = Glide();
            Run(flow, glide, glide, 1.1f); Run(flow, Glide(-40f), Glide(-40f), .6f); Run(flow, Glide(40f), Glide(40f), .6f);
            flow.Begin(false);
            Assert.That(flow.Step, Is.EqualTo(HandCalibrationStep.Return));
            var outOfView = Glide(right: HandPoseSource.Inferred);
            bool done = false;
            for (float t = 0; t < 3.5f && !done; t += .02f) done = flow.Sample(outOfView, outOfView, .02f);
            Assert.That(done, Is.True, "A natural pose outside camera view can still calibrate");
            var calibration = new BirdTrackingCalibration { CompactHands = true };
            Assert.That(calibration.CaptureNaturalHands(outOfView), Is.True);
            Assert.That(calibration.HumanSpanMeters, Is.EqualTo(1.28f).Within(.001f));
        }

        [Test]
        public void InferredMotionEarnsPartialAuthorityOnlyWithACalibratedBiasAfterSettling()
        {
            WingInput Pose(HandPoseSource source, float x, double time) => new WingInput
            { Source = source, Tracked = true, Position = new Vector3(x, 0, .25f), Orientation = Quaternion.identity,
              SampleTimestamp = time, HasUnextrapolatedPose = source == HandPoseSource.DirectHigh,
              UnextrapolatedPosition = new Vector3(x, 0, .25f), UnextrapolatedOrientation = Quaternion.identity, UnextrapolatedTimestamp = time };
            WingInput Drive(HandPoseContinuity f)
            {
                for (int i = 0; i < 12; i++) f.Sample(Pose(HandPoseSource.DirectHigh, -.45f, 1 + i * .02), Vector3.zero, Quaternion.identity, .02f);
                WingInput last = default;
                for (int i = 0; i < 20; i++) last = f.Sample(Pose(HandPoseSource.Inferred, -.45f - i * .02f, 1.3 + i * .02), Vector3.zero, Quaternion.identity, .02f);
                return last;
            }
            var unknown = Drive(new HandPoseContinuity());
            Assert.That(unknown.EstimatedStrokeAuthority, Is.Zero, "Without calibration context inferred motion stays forceless");
            var calibrated = new HandPoseContinuity(); calibrated.SetInferredBias(Vector3.zero);
            var settled = Drive(calibrated);
            Assert.That(settled.MotionEstimated, Is.True, "Still never measured effort or a takeoff stroke");
            Assert.That(settled.EstimatedStrokeAuthority, Is.EqualTo(HandPoseContinuity.InferredStrokeAuthority));
            Assert.That(settled.Velocity.x, Is.EqualTo(-1f).Within(.05f));
        }

        [Test]
        public void EstimatedAuthorityScalesStrokeForceButNeverTakesOff()
        {
            float Force(float authority)
            {
                var input = new StaticInput(); var frame = FlightInputFrame.Neutral;
                frame.LeftWing.Velocity = frame.RightWing.Velocity = Vector3.down * 2.5f;
                frame.LeftWing.MotionEstimated = frame.RightWing.MotionEstimated = authority < 1f;
                frame.LeftWing.EstimatedStrokeAuthority = frame.RightWing.EstimatedStrokeAuthority = authority;
                input.Frame = frame;
                var controller = new BirdFlightController(input, Vector3.up * 20f); controller.Step(1f / 90f);
                return controller.StrokeForce.magnitude;
            }
            Assert.That(Force(0f), Is.Zero);
            Assert.That(Force(.5f), Is.EqualTo(.5f * Force(1f)).Within(.01f * Force(1f)));
            var perched = new StaticInput(); var still = FlightInputFrame.Neutral; perched.Frame = still;
            var profile = BirdFlightProfile.Duck();
            var controller = new BirdFlightController(perched, Vector3.up * (profile.CollisionRadius + .01f),
                profile: profile, environment: new PlaneFlightEnvironment(0));
            for (int i = 0; i < 300 && controller.State.Phase != FlightPhase.Perched; i++) controller.Step(1f / 120f);
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Perched));
            var estimated = still; estimated.LeftWing.Velocity = estimated.RightWing.Velocity = Vector3.down * 3f;
            estimated.LeftWing.MotionEstimated = estimated.RightWing.MotionEstimated = true;
            estimated.LeftWing.EstimatedStrokeAuthority = estimated.RightWing.EstimatedStrokeAuthority = .5f;
            perched.Frame = estimated;
            for (int i = 0; i < 30; i++) controller.Step(1f / 90f);
            Assert.That(controller.TakeoffCount, Is.Zero, "Inferred strokes never lift a perched bird");
        }

        private sealed class StaticInput : IFlightInput
        {
            public FlightInputFrame Frame = FlightInputFrame.Neutral;
            public string Mode => "Static calibration fixture";
            public FlightInputFrame Sample(float deltaTime) => Frame;
        }
    }
}
