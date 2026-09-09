using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class DuckFlightTests
    {
        private sealed class Input : IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Fixture";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        private sealed class ConstantWind : IWindField
        {
            private readonly Vector3 value;
            public ConstantWind(Vector3 value) { this.value=value; }
            public string ModeName=>"Fixture wind";
            public Vector3 Sample(Vector3 position,float time)=>value;
        }
        private static BirdFlightController Run(SyntheticGesture gesture, float seconds=2f, float dt=1f/120f, float amplitude=.3f)
        {
            var c=new BirdFlightController(new SyntheticFlightInput(gesture){FlapAmplitude=amplitude},Vector3.up*20f);
            for(int i=0;i<Mathf.RoundToInt(seconds/dt);i++) c.Step(dt);
            return c;
        }
        [Test] public void StrongFlapsAddMoreEnergyThanWeakAndDoNotYaw()
        {
            var weak=Run(SyntheticGesture.Flap,4f,amplitude:.12f);var strong=Run(SyntheticGesture.Flap,4f);
            Assert.That(strong.MechanicalEnergy,Is.GreaterThan(weak.MechanicalEnergy+10f));
            Assert.That(Mathf.Abs(strong.State.Position.x),Is.LessThan(.01f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,strong.State.Rotation.eulerAngles.y)),Is.LessThan(.1f));
        }
        [Test] public void GlideSpendsEnergyAndHeight()
        {
            var initial=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*20f);
            var glide=Run(SyntheticGesture.Glide);
            Assert.That(glide.MechanicalEnergy,Is.LessThan(initial.MechanicalEnergy));
            Assert.That(glide.State.Position.y,Is.LessThan(20f));
        }
        [Test] public void BankedLiftCurvesVelocityTowardLowerWing()
        {
            var left=Run(SyntheticGesture.BankLeft);var right=Run(SyntheticGesture.BankRight);
            Assert.That(left.State.Velocity.x,Is.LessThan(-1f));Assert.That(right.State.Velocity.x,Is.GreaterThan(1f));
            Assert.That(left.State.Position.x,Is.EqualTo(-right.State.Position.x).Within(.02f));
            Assert.That(left.LiftForce.x,Is.LessThan(0f));
        }
        [Test] public void TuckDescendsFasterAndGainsAirspeed()
        {
            var dive=Run(SyntheticGesture.Dive);var glide=Run(SyntheticGesture.Glide);
            Assert.That(dive.State.Speed,Is.GreaterThan(glide.State.Speed));
            Assert.That(dive.State.Position.y,Is.LessThan(glide.State.Position.y));
            // Compare area at equal initial flow, before trajectories diverge.
            var d=Run(SyntheticGesture.Dive,1f/120);var g=Run(SyntheticGesture.Glide,1f/120);
            Assert.That(d.DragForce.magnitude,Is.LessThan(g.DragForce.magnitude));
        }
        [Test] public void FlareBleedsForwardSpeedAndCannotHover()
        {
            var flare=Run(SyntheticGesture.Flare);var glide=Run(SyntheticGesture.Glide);
            Assert.That(flare.State.Velocity.z,Is.LessThan(glide.State.Velocity.z));
            Assert.That(flare.State.Velocity.y,Is.LessThan(-1f));
        }
        [Test] public void LowSpeedStallFallsAndFlapsRecover()
        {
            var input=new Input();var profile=BirdFlightProfile.Duck();profile.InitialSpeedMps=.2f;
            var c=new BirdFlightController(input,Vector3.up*50f,profile:profile);
            for(int i=0;i<60;i++)c.Step(1f/120f);
            Assert.That(c.State.Velocity.y,Is.LessThan(-2f));
            float energy=c.MechanicalEnergy;
            input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*2f;
            for(int i=0;i<240;i++)c.Step(1f/120f);
            Assert.That(c.State.Velocity.y,Is.GreaterThan(0f));Assert.That(c.MechanicalEnergy,Is.GreaterThan(energy));
        }
        [TestCase(SyntheticGesture.Glide)] [TestCase(SyntheticGesture.Flap)]
        [TestCase(SyntheticGesture.BankLeft)] [TestCase(SyntheticGesture.Dive)]
        [TestCase(SyntheticGesture.Flare)] [TestCase(SyntheticGesture.TrackingLoss)]
        [TestCase(SyntheticGesture.StallRecovery)]
        public void TimestepsRemainFiniteAndClose(SyntheticGesture gesture)
        {
            var fine=Run(gesture,4f,1f/120f);var coarse=Run(gesture,4f,1f/60f);
            Assert.That(float.IsNaN(coarse.State.Speed)||float.IsInfinity(coarse.State.Speed),Is.False);
            Assert.That(Vector3.Distance(fine.State.Position,coarse.State.Position),Is.LessThan(1.5f));
        }
        [Test] public void TrackingLossCannotInjectPhantomFlapOrTuck()
        {
            var input=new Input();input.Frame.LeftWing.Tracked=input.Frame.RightWing.Tracked=false;
            input.Frame.LeftWing.Position=input.Frame.RightWing.Position=Vector3.zero;
            input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*100f;
            var lost=new BirdFlightController(input,Vector3.up*20);
            var neutral=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*20);
            for(int i=0;i<60;i++){lost.Step(1f/60);neutral.Step(1f/60);}
            Assert.That(lost.State.Position,Is.EqualTo(neutral.State.Position));
            input.Frame=FlightInputFrame.Neutral;lost.Step(1f/60);
            Assert.That(lost.State.Phase,Is.EqualTo(FlightPhase.Gliding));
        }
        [Test] public void GroundContactAllowsTakeoffAndKeepsLaunchInertia()
        {
            var input=new Input();var c=new BirdFlightController(input,Vector3.up*.2f,groundHeight:.15f);
            for(int i=0;i<240;i++) c.Step(1f/120f);
            Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
            input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*3;
            c.Step(1f/120f);input.Frame=FlightInputFrame.Neutral;c.Step(1f/120f);
            Assert.That(c.State.Position.y,Is.GreaterThan(.15f));Assert.That(c.State.Velocity.y,Is.GreaterThan(1f));
        }
        [Test] public void IkClampsReachWithoutStretchingAndKeepsBothLengths()
        {
            var elbow=WingRigSolver.Solve(Vector3.zero,Vector3.right*50,Vector3.back,.23f,.22f,out var target);
            Assert.That(elbow.magnitude,Is.EqualTo(.23f).Within(.0001f));
            Assert.That(Vector3.Distance(elbow,target),Is.EqualTo(.22f).Within(.0001f));
            Assert.That(target.magnitude,Is.LessThan(.45f));
        }
        [Test] public void CalibrationPreservesThreeAxesAndRotationWithHumanSpanScale()
        {
            var calibration=new BirdTrackingCalibration();var frame=FlightInputFrame.Neutral;
            frame.HeadPosition=new Vector3(0,1.7f,0);frame.HeadTracked=true;
            Assert.That(calibration.Capture(frame), Is.True);
            var before=calibration.WingTarget(frame.LeftWing,true);
            frame.LeftWing.Position+=new Vector3(.1f,.2f,-.3f);
            var delta=calibration.WingTarget(frame.LeftWing,true)-before;
            Assert.That(Vector3.Distance(delta,new Vector3(.1f,.2f,-.3f)*calibration.MotionScale),Is.LessThan(.0001f));
            frame.LeftWing.Orientation=Quaternion.Euler(30,20,10);
            Assert.That(Quaternion.Angle(calibration.WingRotation(frame.LeftWing,true),frame.LeftWing.Orientation),Is.LessThan(.01f));
            Assert.That(calibration.HeadOffset(frame.HeadPosition),Is.EqualTo(Vector3.zero));
        }

        [Test] public void CalibrationRejectsInvalidHeadAndNarrowStartupPose()
        {
            var calibration = new BirdTrackingCalibration();
            var frame = FlightInputFrame.Neutral;
            Assert.That(calibration.Capture(frame), Is.False);
            frame.HeadTracked = true;
            frame.LeftWing.Position = Vector3.left * .1f;
            frame.RightWing.Position = Vector3.right * .1f;
            Assert.That(calibration.Capture(frame), Is.False);
            Assert.That(calibration.Captured, Is.False);
        }

        [Test] public void HeadTranslationKeepsPhysicalScaleIndependentOfArmScale()
        {
            var calibration = new BirdTrackingCalibration();
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true;
            frame.HeadPosition = new Vector3(0f, 1.7f, 0f);
            frame.LeftWing.Position = Vector3.left * .4f;
            frame.RightWing.Position = Vector3.right * .4f;
            Assert.That(calibration.Capture(frame), Is.True);
            Assert.That(calibration.MotionScale, Is.Not.EqualTo(1f));
            Assert.That(calibration.HeadOffset(frame.HeadPosition + Vector3.forward * .1f).magnitude,
                Is.EqualTo(.1f).Within(.0001f));
        }

        [Test] public void WingOrientationDirectsStrokeReaction()
        {
            var input = new Input();
            input.Frame.LeftWing.Orientation = input.Frame.RightWing.Orientation = Quaternion.Euler(45f, 0f, 0f);
            var normal = input.Frame.LeftWing.Orientation * Vector3.up;
            input.Frame.LeftWing.Velocity = input.Frame.RightWing.Velocity = -normal * 2f;
            var controller = new BirdFlightController(input, Vector3.up * 20f);
            controller.Step(1f / 120f);
            Assert.That(Vector3.Angle(controller.StrokeForce, controller.State.Rotation * normal), Is.LessThan(.1f));
            Assert.That(controller.StrokeForce.z, Is.GreaterThan(1f));
        }

        [Test] public void CalibratedHeadingDoesNotRotateStrokeIntoSideForce()
        {
            var baseInput = new Input();
            var baseController = new BirdFlightController(baseInput, Vector3.up * 20f);
            var neutral = FlightInputFrame.Neutral;
            neutral.HeadOrientation = Quaternion.identity;
            baseController.Calibrate(neutral);
            var actionRotation = Quaternion.Euler(45f, 0f, 0f);
            var actionNormal = actionRotation * Vector3.up;
            baseInput.Frame.LeftWing.Orientation = baseInput.Frame.RightWing.Orientation = actionRotation;
            baseInput.Frame.LeftWing.Velocity = baseInput.Frame.RightWing.Velocity = -actionNormal * 2f;
            baseController.Step(1f / 120f);

            var yaw = Quaternion.Euler(0f, 90f, 0f);
            var yawInput = new Input();
            var yawController = new BirdFlightController(yawInput, Vector3.up * 20f);
            var yawNeutral = FlightInputFrame.Neutral;
            yawNeutral.HeadOrientation = yaw;
            yawNeutral.LeftWing.Position = yaw * neutral.LeftWing.Position;
            yawNeutral.RightWing.Position = yaw * neutral.RightWing.Position;
            yawNeutral.LeftWing.Orientation = yawNeutral.RightWing.Orientation = yaw;
            yawController.Calibrate(yawNeutral);
            yawInput.Frame = yawNeutral;
            yawInput.Frame.LeftWing.Orientation = yawInput.Frame.RightWing.Orientation = yaw * actionRotation;
            yawInput.Frame.LeftWing.Velocity = yawInput.Frame.RightWing.Velocity = yaw * (-actionNormal * 2f);
            yawController.Step(1f / 120f);

            Assert.That(Vector3.Angle(baseController.StrokeForce, yawController.StrokeForce), Is.LessThan(.1f));
            Assert.That(Mathf.Abs(yawController.StrokeForce.x), Is.LessThan(.01f));
        }

        [Test] public void FirstValidHeadSampleEstablishesOneToOneOrigin()
        {
            var calibration = new BirdTrackingCalibration();
            var frame = FlightInputFrame.Neutral;
            frame.HeadPosition = new Vector3(.2f, 1.6f, -.1f);
            Assert.That(calibration.CaptureHead(frame), Is.False);
            frame.HeadTracked = true;
            Assert.That(calibration.CaptureHead(frame), Is.True);
            Assert.That(calibration.HeadOffset(frame.HeadPosition), Is.EqualTo(Vector3.zero));
            Assert.That(calibration.HeadOffset(frame.HeadPosition + Vector3.up * .12f).y,
                Is.EqualTo(.12f).Within(.0001f));
        }

        [Test] public void WindModesExposeHelpfulThermalsHazardsAndStillAir()
        {
            var go=new GameObject("WindFixture");var field=go.AddComponent<WindField>();
            field.SetMode(WindMode.Assisted);
            Assert.That(field.Sample(WindField.ThermalCenters[0]+Vector3.up*8f,1f).y,Is.GreaterThan(3f));
            field.SetMode(WindMode.Wild);
            Assert.That(field.Sample(WindField.ThermalCenters[2]+Vector3.up*8f,1f).y,Is.LessThan(-2f));
            field.SetMode(WindMode.StillAir);
            Assert.That(field.Sample(Vector3.one*99f,123f),Is.EqualTo(Vector3.zero));
            Object.DestroyImmediate(go);
        }

        [Test] public void UpdraftPreservesMoreHeightThanStillAir()
        {
            var calm=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*20f,wind:new ConstantWind(Vector3.zero));
            var lifted=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*20f,wind:new ConstantWind(Vector3.up*3f));
            for(int i=0;i<240;i++){calm.Step(1f/120f);lifted.Step(1f/120f);}
            Assert.That(lifted.State.Position.y,Is.GreaterThan(calm.State.Position.y+1f));
            Assert.That(lifted.WindVelocity.y,Is.EqualTo(3f));
        }

        [Test] public void PlatformRecenterInvalidatesOldWingFrameButCanCaptureNewComfortPose()
        {
            var calibration=new BirdTrackingCalibration();var frame=FlightInputFrame.Neutral;
            frame.HeadTracked=true;frame.HeadPosition=Vector3.up*1.65f;
            Assert.That(calibration.Capture(frame),Is.True);
            calibration.BeginPlatformRecenter();
            Assert.That(calibration.Captured,Is.False);Assert.That(calibration.HeadCaptured,Is.False);
            frame.HeadPosition+=new Vector3(.2f,0f,.1f);
            frame.LeftWing.Position=new Vector3(-.75f,1.25f,.15f);
            frame.RightWing.Position=new Vector3(.75f,1.25f,.15f);
            Assert.That(calibration.Capture(frame),Is.True);
            Assert.That(calibration.WingTarget(frame.LeftWing,true),Is.EqualTo(new Vector3(-.56f,.04f,.005f)));
        }

        [Test] public void ComfortableGlidePoseRejectsUnevenOrForwardHands()
        {
            var calibration=new BirdTrackingCalibration();var frame=FlightInputFrame.Neutral;
            frame.HeadTracked=true;frame.HeadPosition=new Vector3(0f,1.65f,0f);
            frame.LeftWing.Position=new Vector3(-.7f,1.25f,.1f);
            frame.RightWing.Position=new Vector3(.7f,1.25f,.1f);
            Assert.That(calibration.IsComfortableGlidePose(frame),Is.True);
            frame.LeftWing.Position+=Vector3.up*.3f;
            Assert.That(calibration.IsComfortableGlidePose(frame),Is.False);
            frame.LeftWing.Position=new Vector3(-.7f,1.25f,.8f);
            frame.RightWing.Position=new Vector3(.7f,1.25f,.8f);
            Assert.That(calibration.IsComfortableGlidePose(frame),Is.False);
        }

        [Test] public void FlareExtendsReadablePerchCaptureEnvelope()
        {
            var perch=new[]{new PerchInfo(new Vector3(0f,20f,1.9f),Quaternion.identity)};
            var glideInput=new Input();var glide=new BirdFlightController(glideInput,Vector3.up*20f,perch);
            glide.Step(1f/120f);Assert.That(glide.State.Phase,Is.Not.EqualTo(FlightPhase.Perching));
            var flareInput=new Input();flareInput.Frame.Flare=1f;
            var flare=new BirdFlightController(flareInput,Vector3.up*20f,perch);
            flare.Step(1f/120f);Assert.That(flare.State.Phase,Is.EqualTo(FlightPhase.Perching));
        }
    }
}
