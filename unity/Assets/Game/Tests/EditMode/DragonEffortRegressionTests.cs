using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class DragonEffortRegressionTests
    {
        private sealed class HumanArc : IFlightInput
        {
            private readonly TrackedBodyFrame body=new TrackedBodyFrame();
            public float Amplitude,Frequency,Twist,HeadPitch;
            public bool Cyclic;
            public Quaternion LastRawOrientation { get; private set; }
            private float time;
            public string Mode => "Calibrated ergonomic wrist regression";
            public FlightInputFrame Sample(float dt)
            {
                time+=dt;
                float phase=time*Mathf.PI*2*Frequency;
                float height=Amplitude*Mathf.Sin(phase);
                float reach=Mathf.Sqrt(.65f*.65f-height*height);
                float twist=dt<=0 ? 0 : Twist*(Cyclic ? Mathf.Sin(phase) : 1);
                var frame=FlightInputFrame.Neutral;
                frame.HeadTracked=true;frame.HeadPosition=Vector3.up*1.6f;
                frame.LookDirection=Quaternion.Euler(-HeadPitch,0,0)*Vector3.forward;
                frame.LeftWing.Position=new Vector3(-reach,1.35f+height,0);
                frame.RightWing.Position=new Vector3(reach,1.35f+height,0);
                frame.LeftWing.Orientation=frame.RightWing.Orientation=Quaternion.Euler(twist,0,0);
                LastRawOrientation=frame.LeftWing.Orientation;
                body.Sample(ref frame,dt);return frame;
            }
        }
        // The previous full-gain Dragon lost24–50m in these ten-second wrist-pitched
        // strokes. Exercise real controller derivatives after a separate neutral capture.
        [TestCase(.2f,.8f,20f,false)]
        [TestCase(.3f,1.1f,20f,false)]
        [TestCase(.4f,.8f,20f,false)]
        [TestCase(.4f,1.5f,20f,false)]
        [TestCase(.3f,1.1f,20f,true)]
        [TestCase(.3f,1.1f,0f,false)]
        public void DragonNaturalWristPitchDoesNotTurnOrdinaryWingbeatsIntoSevereDive(float amplitude,float frequency,float twist,bool cyclic)
        {
            var input=new HumanArc { Amplitude=amplitude,Frequency=frequency,Twist=twist,Cyclic=cyclic };
            var profile=Resources.Load<BirdCharacterDefinition>("Characters/Dragon").BuildProfile();
            var flight=new BirdFlightController(input,Vector3.up*100,profile:profile);
            flight.Calibrate(input.Sample(0));
            for(int i=0;i<1200;i++)
            {
                flight.Step(1f/120);
                Assert.That(flight.LandingBrake,Is.Zero);
                Assert.That(flight.HeadPitchInput,Is.Zero);
                Assert.That(flight.State.Position.y,Is.GreaterThan(99),"Moderate real strokes with ordinary wrist pitch must sustain flight within a one-metre transient loss.");
                Assert.That(Quaternion.Angle(flight.LastInput.LeftWing.Orientation,input.LastRawOrientation),Is.LessThan(.01f),
                    "Aerodynamic sensitivity must not rewrite controller orientation used by articulation.");
            }
            if(twist==0) Assert.That(flight.State.Position.y,Is.GreaterThan(100));
        }
        [Test] public void DragonDeliberateHeadDownStillCommandsUsefulDescent()
        {
            var input=new HumanArc { Frequency=.8f,Twist=20 };
            var profile=Resources.Load<BirdCharacterDefinition>("Characters/Dragon").BuildProfile();
            var flight=new BirdFlightController(input,Vector3.up*100,profile:profile);
            flight.Calibrate(input.Sample(0));input.HeadPitch=-23;
            for(int i=0;i<600;i++) flight.Step(1f/120);
            Assert.That(flight.HeadPitchInput,Is.EqualTo(-1).Within(.001));
            Assert.That(flight.State.Position.y,Is.LessThan(95));
            Assert.That(Quaternion.Angle(flight.LastInput.LeftWing.Orientation,Quaternion.Euler(20,0,0)),Is.LessThan(.01));
        }
        [Test] public void WristSensitivityIsSpeciesSpecificAndDoesNotChangeNeutralGlide()
        {
            var duck=Resources.Load<BirdCharacterDefinition>("Characters/Duck").BuildProfile();
            Assert.That(duck.WingPitchSensitivity,Is.EqualTo(1));
            Assert.That(BirdFlightProfile.Duck().WingPitchSensitivity,Is.EqualTo(1));
            var definition=Resources.Load<BirdCharacterDefinition>("Characters/Dragon");
            var tuned=definition.BuildProfile();var originalSensitivity=definition.BuildProfile();
            Assert.That(tuned.WingPitchSensitivity,Is.LessThan(1));
            originalSensitivity.WingPitchSensitivity=1;
            var a=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100,profile:tuned);
            var b=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100,profile:originalSensitivity);
            for(int i=0;i<1200;i++) { a.Step(1f/120);b.Step(1f/120); }
            Assert.That(a.State.Position,Is.EqualTo(b.State.Position));
            Assert.That(a.State.Velocity,Is.EqualTo(b.State.Velocity));
        }
    }
}
