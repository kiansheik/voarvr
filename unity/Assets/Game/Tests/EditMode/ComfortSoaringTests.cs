using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.Gameplay;
using VoarVR.Telemetry;
namespace VoarVR.Tests
{
    public class ComfortSoaringTests
    {
        private sealed class Input:IFlightInput {public FlightInputFrame Frame=FlightInputFrame.Neutral;public string Mode=>"comfort test";public FlightInputFrame Sample(float dt)=>Frame;}
        private sealed class Air:IWindField,IWindAssistance {public string ModeName=>"Assisted fixture";public bool AutomaticFeathering=>true;public Vector3 Sample(Vector3 p,float t)=>new Vector3(0,3.4f,0);}
        [TestCase(.04f)][TestCase(.06f)][TestCase(.10f)][TestCase(.15f)]
        public void RelaxedArmSpanRetainsAssistedProtection(float relaxation)
        {
            var p=Resources.Load<BirdCharacterDefinition>("Characters/Magpie").BuildProfile();var input=new Input();var c=new BirdFlightController(input,Vector3.up*100,profile:p,wind:new Air());
            input.Frame.LeftWing.Position*=1-relaxation;input.Frame.RightWing.Position*=1-relaxation;
            input.Frame.LookDirection=Quaternion.Euler(-6,0,0)*Vector3.forward;
            float stalled=0;for(int i=0;i<1200;i++){c.Step(1f/120);if(c.IsStalled)stalled+=1f/120;}
            Assert.That(c.FeatherSupport,Is.GreaterThan(.99f));Assert.That(stalled,Is.LessThan(1));Assert.That(c.State.Position.y,Is.GreaterThan(100));
            input.Frame.Tuck=1;c.Step(.02f);Assert.That(c.FeatherSupport,Is.Zero,"Deliberate trigger tuck must remain authoritative");
        }
        [Test] public void SoftAdvancedCenterRejectsSmallUnintentionalMotion()
        {Assert.That(AcrobaticDynamics.SoftInput(3,4,28),Is.Zero);Assert.That(AcrobaticDynamics.SoftInput(10,4,28),Is.LessThan(.1f));Assert.That(AcrobaticDynamics.SoftInput(28,4,28),Is.EqualTo(1));}
        [Test] public void ForagingSweepsAndComboAreBounded()
        {Assert.That(ForagingScore.Touches(Vector3.zero,Vector3.forward*20,Vector3.forward*10,2),Is.True);Assert.That(ForagingScore.Touches(Vector3.zero,Vector3.forward*20,Vector3.right*5,2),Is.False);var score=new ForagingScore();for(int i=0;i<20;i++)score.Catch(i);Assert.That(score.Combo,Is.EqualTo(5));Assert.That(score.Catch(30),Is.EqualTo(10));}
        [Test] public void EffortExcludesTrackingGapsAndRecognizesQuietRest()
        {var e=new FlightEffort();var f=FlightInputFrame.Neutral;f.LeftWing.Velocity=f.RightWing.Velocity=Vector3.down;for(int i=0;i<100;i++)e.Step(f,.02f,true);Assert.That(e.ActiveSeconds,Is.EqualTo(2).Within(.001));e.Step(f,.02f,false);Assert.That(e.ActiveSeconds,Is.EqualTo(2).Within(.001));f.LeftWing.Velocity=f.RightWing.Velocity=Vector3.zero;for(int i=0;i<200;i++)e.Step(f,.02f,true);Assert.That(e.Resting,Is.True);}
    }
}
