using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class AssistedFeatheringTests
    {
        private sealed class Input:IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Assisted air regression";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        private sealed class ZeroAssistedAir:IWindField,IWindAssistance
        {
            public string ModeName=>"Zero air with assistance enabled";
            public bool AutomaticFeathering=>true;
            public Vector3 Sample(Vector3 position,float time)=>Vector3.zero;
        }
        private GameObject root;
        private WindField wind;
        [SetUp] public void Setup() { root=new GameObject("Assisted air fixture");wind=root.AddComponent<WindField>(); }
        [TearDown] public void Teardown()=>Object.DestroyImmediate(root);
        private static Vector3 Core()
        {
            var plume=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);
            var core=plume.Center(0,plume.Altitude);
            return new Vector3((float)core.X,(float)core.Y,(float)core.Z);
        }
        private static BirdFlightProfile Profile(string species)=>Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();

        [TestCase("Duck")][TestCase("Dragon")]
        public void ActualFiniteCoreSupportsUnpoweredCirclingForBothSpecies(string species)
        {
            var input=new Input();input.Frame.Bank=.55f;
            var spawn=Core()+Vector3.left*8;
            var flight=new BirdFlightController(input,spawn,profile:Profile(species),wind:wind);
            var still=new BirdFlightController(input,spawn,profile:Profile(species));
            for(int i=0;i<2400;i++)
            {
                flight.Step(1f/120);still.Step(1f/120);
                Assert.That(flight.StrokeForce,Is.EqualTo(Vector3.zero),"Thermal benefit cannot come from invented wing work.");
            }
            Assert.That(flight.State.Position.y,Is.GreaterThan(spawn.y+10));
            Assert.That(flight.State.Position.y,Is.GreaterThan(still.State.Position.y+10));
        }
        [TestCase("Duck")][TestCase("Dragon")]
        public void FlyingIntoRealAssistedAirDoesNotCollapseIntoDeepStall(string species)
        {
            var spawn=Core()+new Vector3(0,10,-80);
            var flight=new BirdFlightController(new Input(),spawn,profile:Profile(species),wind:wind);
            for(int i=0;i<4800;i++)
            {
                flight.Step(1f/120);
                Assert.That(flight.State.Position.y,Is.GreaterThan(spawn.y-15),"Ordinary entry must not trigger the previous unrecovering hundreds-of-metres drop.");
                Assert.That(flight.StrokeForce,Is.EqualTo(Vector3.zero));
            }
            Assert.That(flight.State.Position.y,Is.GreaterThan(spawn.y));
        }
        [TestCase(WindMode.Assisted,true)]
        [TestCase(WindMode.StillAir,false)]
        [TestCase(WindMode.Touring,false)]
        [TestCase(WindMode.Wild,false)]
        public void FeatheringIsExplicitlyAssistedOnly(WindMode mode,bool enabled)
        {
            wind.SetMode(mode);
            Assert.That(((IWindAssistance)wind).AutomaticFeathering,Is.EqualTo(enabled));
        }
        [Test] public void ZeroAirAssistanceLeavesDuckTrajectoryBitIdentical()
        {
            var a=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100);
            var b=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100,wind:new ZeroAssistedAir());
            for(int i=0;i<2400;i++)
            {
                a.Step(1f/120);b.Step(1f/120);
                Assert.That(b.State.Position,Is.EqualTo(a.State.Position));
                Assert.That(b.State.Velocity,Is.EqualTo(a.State.Velocity));
                Assert.That(b.WingFeatherDeg,Is.Zero);
            }
        }
        [TestCase("Duck")][TestCase("Dragon")]
        public void DeliberateHeadDownStillProducesUsefulDescentRelativeToGlide(string species)
        {
            var input=new Input();var spawn=Core()+Vector3.left*8;
            var dive=new BirdFlightController(input,spawn,profile:Profile(species),wind:wind);
            var glide=new BirdFlightController(new Input(),spawn,profile:Profile(species),wind:wind);
            dive.Calibrate(input.Frame);
            input.Frame.LookDirection=Quaternion.Euler(30,0,0)*Vector3.forward;
            for(int i=0;i<600;i++) { dive.Step(1f/120);glide.Step(1f/120); }
            Assert.That(dive.HeadPitchInput,Is.EqualTo(-1).Within(.001));
            Assert.That(dive.LastInput.LookDirection,Is.EqualTo(input.Frame.LookDirection));
            Assert.That(dive.State.Position.y,Is.LessThan(glide.State.Position.y-5));
        }
        [TestCase(false)][TestCase(true)]
        public void DeliberateBrakeOrTuckBypassesAutomaticFeathering(bool tuck)
        {
            var input=new Input();input.Frame.Flare=tuck ? 0 : 1;input.Frame.Tuck=tuck ? 1 : 0;
            var flight=new BirdFlightController(input,Core()+Vector3.left*8,wind:wind);
            for(int i=0;i<240;i++)
            {
                flight.Step(1f/120);
                Assert.That(flight.WingFeatherDeg,Is.Zero);
                Assert.That(flight.LandingBrake,Is.EqualTo(tuck ? 0 : 1));
            }
            Assert.That(flight.State.Phase,Is.EqualTo(tuck ? FlightPhase.Diving : FlightPhase.Flaring));
        }
    }
}
