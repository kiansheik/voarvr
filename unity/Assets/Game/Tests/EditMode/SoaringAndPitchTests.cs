using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class SoaringAndPitchTests
    {
        private sealed class Input : IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Pitch fixture";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        private static Vector3 Look(float elevation)=>Quaternion.Euler(-elevation,0,0)*Vector3.forward;
        [TestCase(0f)] [TestCase(-12f)] [TestCase(9f)]
        public void PitchIsRelativeToComfortableNeutral(float neutral)
        {
            var input=new Input(); input.Frame.LookDirection=Look(neutral);
            var c=new BirdFlightController(input,Vector3.up*100);
            c.Calibrate(input.Frame); c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.EqualTo(0).Within(.0001));
            input.Frame.LookDirection=Look(neutral-3f); c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.EqualTo(0).Within(.0001));
            input.Frame.LookDirection=Look(neutral-12f); c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.InRange(-.5f,-.35f));
            input.Frame.LookDirection=Look(neutral-23f); c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.EqualTo(-1).Within(.001));
            input.Frame.LookDirection=Look(neutral+12f); c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.InRange(.2f,.35f));
        }
        private static Vector3 Glide(string species)
        {
            var p=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
            var c=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100,profile:p);
            float time=0,energy=c.MechanicalEnergy;
            while(c.State.Position.y>90 && time<120)
            {
                c.Step(1f/120);time+=1f/120;
                Assert.That(c.MechanicalEnergy,Is.LessThanOrEqualTo(energy+.002f),"Still-air glide cannot create energy");
                energy=c.MechanicalEnergy;
            }
            Assert.That(time,Is.LessThan(120f));
            return new Vector3(c.State.Position.z,(100-c.State.Position.y)/time,c.State.Speed);
        }
        [Test] public void DragonGlidesFartherWithLowerSinkWhileDuckBaselineIsPreserved()
        {
            var duck=Glide("Duck");var dragon=Glide("Dragon");
            Assert.That(duck.x,Is.EqualTo(43.12234f).Within(.05f));
            Assert.That(duck.y,Is.EqualTo(1.472212f).Within(.005f));
            Assert.That(duck.z,Is.EqualTo(6.387836f).Within(.005f));
            Assert.That(dragon.x/duck.x,Is.InRange(1.5f,2.15f));
            Assert.That(dragon.y,Is.LessThan(duck.y*.6f));
            Assert.That(dragon.y,Is.GreaterThan(.15f));
        }
        [Test] public void LostHeadTrackingDoesNotSteerAwayFromCalibratedNeutral()
        {
            var input=new Input();input.Frame.HeadTracked=true;input.Frame.LookDirection=Look(-12);
            var c=new BirdFlightController(input,Vector3.up*100);c.Calibrate(input.Frame);
            input.Frame.HeadTracked=false;input.Frame.LookDirection=Vector3.forward;c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.Zero);
            input.Frame.HeadTracked=true;input.Frame.LookDirection=Look(-24);c.Step(.02f);
            Assert.That(c.HeadPitchInput,Is.LessThan(-.35f));
        }
        [Test] public void UnsafeImpactCannotAutoLandOnFollowingSubsteps()
        {
            var p=BirdFlightProfile.Duck();p.InitialSpeedMps=20;p.LiftSlopePerRadian=0;
            var c=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*.23f,profile:p,groundHeight:.22f);
            for(int i=0;i<120;i++)c.Step(1f/120);
            Assert.That(c.CollisionCount,Is.GreaterThan(0));
            Assert.That(c.LandingCount,Is.Zero);
            Assert.That(c.MissedLanding,Is.True);
            Assert.That(c.State.Phase,Is.Not.EqualTo(FlightPhase.Perched));
        }
        [TestCase("Duck")] [TestCase("Dragon")]
        public void SlowRaisedRecoveryInOpenAirDoesNotActivateLandingBrake(string species)
        {
            var input=new Input();var profile=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
            var c=new BirdFlightController(input,Vector3.up*100,profile:profile,environment:new PlaneFlightEnvironment(0));
            c.Calibrate(input.Frame);
            // A slow recovery followed by a rest at the top of the stroke.
            for(int i=0;i<240;i++)
            {
                float t=i/120f;
                input.Frame.LeftWing.Position.y=input.Frame.RightWing.Position.y=Mathf.Min(.25f,t*.25f);
                input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=t<1 ? Vector3.up*.25f : Vector3.zero;
                c.Step(1f/120);
                Assert.That(c.LandingBrake,Is.Zero,"Open-air rest must retain ordinary glide drag");
            }
            input.Frame.Flare=1;c.Step(1f/120);
            Assert.That(c.LandingBrake,Is.EqualTo(1),"Explicit trigger brake remains available everywhere");
        }
        [Test] public void HeldRaisedSpreadBrakesButOrdinaryWingbeatsDoNot()
        {
            var input=new Input();var c=new BirdFlightController(input,Vector3.up*5,environment:new PlaneFlightEnvironment(0));
            c.Calibrate(input.Frame);
            input.Frame.LeftWing.Position.y=input.Frame.RightWing.Position.y=.22f;
            for(int i=0;i<70;i++)c.Step(.02f);
            Assert.That(c.LandingBrake,Is.EqualTo(1f));
            var flap=new BirdFlightController(new SyntheticFlightInput(SyntheticGesture.Flap),Vector3.up*100);
            for(int i=0;i<300;i++){flap.Step(.02f);Assert.That(flap.LandingBrake,Is.EqualTo(0f));}
        }
    }
}
