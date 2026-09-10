using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class ComfortAndGroundTests
    {
        private sealed class Input : IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Ground controls";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        [TestCase("Duck")] [TestCase("Dragon")]
        public void AutomaticLandingWalkStopAndFlapTakeoff(string species)
        {
            var input=new Input();
            var p=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
            var c=new BirdFlightController(input,Vector3.up*(p.CollisionRadius+.01f),profile:p,environment:new PlaneFlightEnvironment(0));
            for(int i=0;i<300 && c.State.Phase!=FlightPhase.Perched;i++)c.Step(1f/120);
            Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
            var start=c.State.Position;
            input.Frame.GroundMove=Vector2.up;
            for(int i=0;i<120;i++)c.Step(1f/120);
            Assert.That(c.State.Position.z-start.z,Is.EqualTo(p.WalkSpeedMps).Within(.01f));
            Assert.That(c.State.Position.y,Is.EqualTo(p.CollisionRadius+FlightContactSolver.Skin).Within(.001));
            Assert.That(c.LandingCount,Is.EqualTo(1));
            input.Frame.GroundMove=Vector2.zero;c.Step(1f/120);
            Assert.That(c.GroundVelocity,Is.EqualTo(Vector3.zero));
            Assert.That(c.LastImpactSpeed,Is.Zero,"Resting support must not produce repeated crash cues");
            input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*2;
            c.Step(1f/120);
            Assert.That(c.State.Phase,Is.Not.EqualTo(FlightPhase.Perched));
            Assert.That(c.State.Velocity.y,Is.GreaterThan(0));
            Assert.That(c.LandingCount,Is.EqualTo(1));
        }
        [TestCase(-12f)] [TestCase(9f)]
        public void NeutralCameraPitchIsCapturedAgainForEveryCalibration(float pitch)
        {
            var frame=FlightInputFrame.Neutral;frame.HeadTracked=true;
            frame.HeadOrientation=Quaternion.Euler(pitch,35,0);
            var calibration=new BirdTrackingCalibration();
            Assert.That(calibration.Capture(frame),Is.True);
            var relative=Quaternion.Inverse(calibration.NeutralLookPitch)*Quaternion.Inverse(calibration.Heading)*frame.HeadOrientation;
            Assert.That((relative*Vector3.forward).y,Is.EqualTo(0).Within(.001));
            frame.HeadOrientation=Quaternion.Euler(pitch+6,35,0);
            var delta=Quaternion.Inverse(calibration.NeutralLookPitch)*Quaternion.Inverse(calibration.Heading)*frame.HeadOrientation;
            Assert.That(Mathf.Asin((delta*Vector3.forward).y)*Mathf.Rad2Deg,Is.EqualTo(-6).Within(.01));
            Assert.That(calibration.Capture(frame),Is.True);
            relative=Quaternion.Inverse(calibration.NeutralLookPitch)*Quaternion.Inverse(calibration.Heading)*frame.HeadOrientation;
            Assert.That((relative*Vector3.forward).y,Is.EqualTo(0).Within(.001));
        }
        [Test] public void FeedbackHasQuietFloorAndBoundedWindBelowImpacts()
        {
            Assert.That(FlightFeedback.ImpactAmplitude(.1f),Is.Zero);
            Assert.That(FlightFeedback.WindAmplitude(Vector3.one),Is.Zero);
            Assert.That(FlightFeedback.WindAmplitude(Vector3.up*10),Is.GreaterThan(FlightFeedback.WindAmplitude(Vector3.up*5)));
            Assert.That(FlightFeedback.WindAmplitude(Vector3.up*100),Is.LessThanOrEqualTo(.16f));
            Assert.That(FlightFeedback.ImpactAmplitude(100),Is.EqualTo(.65f));
        }
    }
}
