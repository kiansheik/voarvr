using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;
namespace VoarVR.Tests
{
    public class AcrobaticFlightTests
    {
        private sealed class Input:IFlightInput {public FlightInputFrame Frame=FlightInputFrame.Neutral;public string Mode=>"test";public FlightInputFrame Sample(float dt)=>Frame;}
        [TestCase("Duck")][TestCase("Dragon")][TestCase("Magpie")]
        public void IntegratedDiveSpeedLoopUsesRealTrajectoryAndNoFreeEnergy(string species)
        {
            var p=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();p.InitialSpeedMps=22; // Explicit energetic entry condition, not a species buff.
            var input=new Input();input.Frame.LeftWing.Orientation=input.Frame.RightWing.Orientation=Quaternion.Euler(-32,0,0);
            var c=new BirdFlightController(input,Vector3.up*500,profile:p);c.SetControlMode(FlightControlMode.Acrobatic);
            float energy=c.MechanicalEnergy;bool backwards=false,inverted=false,loop=false;
            for(int i=0;i<2400;i++){c.Step(1f/120);Assert.That(c.MechanicalEnergy,Is.LessThanOrEqualTo(energy+.01f));energy=c.MechanicalEnergy;backwards|=c.State.Velocity.z<-.5f;inverted|=(c.State.Rotation*Vector3.up).y<-.9f;loop|=c.Tricks.Last==FlightTrick.BackLoop;}
            Assert.That(backwards && inverted,Is.True);Assert.That(loop,Is.True,"A complete trajectory loop must be detected during the maneuver");
        }
        [TestCase(0f)][TestCase(90f)][TestCase(180f)]
        public void NeutralTorsoYawDoesNotCommandSweepTorque(float yaw)
        {
            var input=new Input();var f=FlightInputFrame.Neutral;f.HeadTracked=f.BodyTracked=true;f.BodyOrientation=Quaternion.Euler(0,yaw,0);f.HeadOrientation=f.BodyOrientation;
            f.LeftWing.Position=f.BodyOrientation*f.LeftWing.Position;f.RightWing.Position=f.BodyOrientation*f.RightWing.Position;f.LeftWing.Orientation=f.RightWing.Orientation=f.BodyOrientation;
            input.Frame=f;var c=new BirdFlightController(input,Vector3.up*100);c.Calibrate(f);c.SetControlMode(FlightControlMode.Acrobatic);c.Step(.02f);Assert.That(c.Acrobatic.ControlTorque.y,Is.EqualTo(0).Within(.0001f));
        }
        [Test] public void ComfortHeadingCannotJumpOnInvertedExit()
        {float yaw=0;for(int i=0;i<120;i++){float next=BirdFlightDriver.AdvanceComfortYaw(yaw,180,1f/120);Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw,next)),Is.LessThanOrEqualTo(.376f));yaw=next;}}
        [TestCase("Duck",0,468.14000f,124.24860f)]
        [TestCase("Dragon",0,487.28370f,99.60699f)]
        [TestCase("Magpie",0,467.83040f,117.45900f)]
        [TestCase("Duck",.6f,461.83160f,7.11151f)]
        [TestCase("Dragon",.6f,482.95260f,6.43829f)]
        [TestCase("Magpie",.6f,459.34540f,-3.17000f)]
        public void BeginnerMatchesOriginalCommitTrajectories(string species,float bank,float y,float z)
        {var input=new Input();input.Frame.Bank=bank;var p=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();var c=new BirdFlightController(input,Vector3.up*500,profile:p);for(int i=0;i<2400;i++)c.Step(1f/120);Assert.That(c.State.Position.y,Is.EqualTo(y).Within(.001f));Assert.That(c.State.Position.z,Is.EqualTo(z).Within(.001f));}
        [Test] public void DefaultAndExplicitBeginnerAreIdentical()
        {var input=new Input();var a=new BirdFlightController(input,Vector3.up*100);var b=new BirdFlightController(input,Vector3.up*100);b.SetControlMode(FlightControlMode.Beginner);for(int i=0;i<1200;i++){input.Frame.Bank=Mathf.Sin(i*.01f)*.3f;a.Step(1f/120);b.Step(1f/120);Assert.That(a.State.Position,Is.EqualTo(b.State.Position));Assert.That(a.State.Rotation,Is.EqualTo(b.State.Rotation));}}
        [Test] public void ModeTransitionDoesNotTeleportOrInjectVelocity()
        {var c=new BirdFlightController(new Input(),Vector3.up*100);c.Step(.02f);var before=c.State;c.SetControlMode(FlightControlMode.Acrobatic);Assert.That(c.State.Position,Is.EqualTo(before.Position));Assert.That(c.State.Velocity,Is.EqualTo(before.Velocity));Assert.That(c.State.Rotation,Is.EqualTo(before.Rotation));c.SetControlMode(FlightControlMode.Beginner);Assert.That(c.State.Rotation,Is.EqualTo(before.Rotation));}
        [Test] public void AdvancedRollActuallyInvertsAndRotatesFully()
        {var d=new AcrobaticDynamics();var p=AcrobaticProfile.ForMass(.2175f);var q=Quaternion.identity;bool inverted=false;float total=0;for(int i=0;i<500;i++){var next=d.Step(q,Vector3.forward,10,0,1f/120,p);total+=Quaternion.Angle(q,next);q=next;inverted|=(q*Vector3.up).y<-.9f;}Assert.That(inverted,Is.True);Assert.That(total,Is.GreaterThan(360));}
        [Test] public void AngularMotionDampsAndSpeciesDiffer()
        {float mag=Rate(.2175f),duck=Rate(1.1f),dragon=Rate(12);Assert.That(mag,Is.GreaterThan(duck));Assert.That(duck,Is.GreaterThan(dragon));var d=new AcrobaticDynamics();var p=AcrobaticProfile.ForMass(1.1f);var q=Quaternion.identity;for(int i=0;i<120;i++)q=d.Step(q,Vector3.forward,10,0,1f/120,p);float before=d.AngularVelocity.magnitude;for(int i=0;i<240;i++)q=d.Step(q,Vector3.zero,10,0,1f/120,p);Assert.That(d.AngularVelocity.magnitude,Is.LessThan(before*.1f));}
        static float Rate(float mass){var d=new AcrobaticDynamics();var p=AcrobaticProfile.ForMass(mass);var q=Quaternion.identity;for(int i=0;i<120;i++)q=d.Step(q,Vector3.forward,10,0,1f/120,p);return d.AngularVelocity.magnitude;}
        [Test] public void TrickRequiresActualFullRotationAndLoopTrajectory()
        {var t=new TrickDetector();for(int i=0;i<=360;i++)t.Step(Quaternion.Euler(0,0,i),Vector3.forward*8,.01f,true);Assert.That(t.Last,Is.EqualTo(FlightTrick.FullRoll));t.Reset(true);for(int i=0;i<=360;i++)t.Step(Quaternion.Euler(i,0,0),Vector3.forward*8,.01f,true);Assert.That(t.Count,Is.Zero,"Body flip alone is not a flown loop");t.Reset(true);for(int i=0;i<=360;i++){var q=Quaternion.Euler(-i,0,0);t.Step(q,q*Vector3.forward*8,.01f,true);}Assert.That(t.Last,Is.EqualTo(FlightTrick.BackLoop));}
        [Test] public void OscillationAndResetDoNotProduceTricks()
        {var t=new TrickDetector();for(int i=0;i<1000;i++)t.Step(Quaternion.Euler(0,0,Mathf.Sin(i*.02f)*40),Vector3.forward*8,.01f,true);Assert.That(t.Count,Is.Zero);t.Step(Quaternion.Euler(0,0,180),Vector3.forward,.02f,false);Assert.That(t.Count,Is.Zero);}
        [Test] public void MarkerChordCannotToggleMode()
        {var g=new ControlModeGesture();Assert.That(g.Sample(true,true,1),Is.False);Assert.That(g.Sample(true,false,1),Is.False);g.Sample(false,false,.01f);Assert.That(g.Sample(true,false,.8f),Is.True);Assert.That(g.Sample(true,false,1),Is.False);}
        [Test] public void ThermalAssistIsBoundedAndPreservesPlayerAuthority()
        {foreach(float player in new[]{-.8f,-.2f,.2f,.8f}){float a=ThermalCentering.Bias(Vector3.right*20,Quaternion.identity,player,4,0);Assert.That(Mathf.Abs(a),Is.LessThanOrEqualTo(.1f));Assert.That(Mathf.Abs(a),Is.LessThanOrEqualTo(Mathf.Abs(player)*.25f));Assert.That(Mathf.Sign(player+a),Is.EqualTo(Mathf.Sign(player)));}Assert.That(ThermalCentering.Bias(Vector3.right,Quaternion.identity,.5f,0,0),Is.Zero);Assert.That(ThermalCentering.Bias(Vector3.right,Quaternion.identity,.5f,4,1),Is.Zero);}
        [Test] public void VerticalAirAndIslandIdentityAreDeterministic()
        {var a=FlightRegions.Island(7319,-4,8);var b=FlightRegions.Island(7319,-4,8);Assert.That(a.X,Is.EqualTo(b.X));Assert.That(a.Y,Is.GreaterThan(215));Assert.That(FlightRegions.HighAir(100,120,150,50,7319,WindMode.StillAir,1),Is.EqualTo(Vector3.zero));Assert.That(FlightRegions.HighAir(100,120,150,50,7319,WindMode.Assisted,1).y,Is.GreaterThan(3));Assert.That(FlightRegions.Biome(180),Is.EqualTo(AltitudeBiome.CloudSea));}
    }
}
