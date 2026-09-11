using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
namespace VoarVR.Tests
{
    public class AdvancedNeutralTests
    {
        [TestCase(.2175f,-5f)][TestCase(.2175f,5f)]
        [TestCase(1.1f,-5f)][TestCase(1.1f,5f)]
        [TestCase(12f,-5f)][TestCase(12f,5f)]
        public void CalibratedNeutralDoesNotRequirePitchCorrectionAgainstInclinedAir(float mass,float vertical)
        {
            var d=new AcrobaticDynamics();var p=AcrobaticProfile.ForMass(mass);var q=Quaternion.identity;
            var air=new Vector3(0,vertical,8);
            for(int i=0;i<360;i++)q=d.Step(q,Vector3.zero,air.magnitude,0,1f/120,p,air);
            Assert.That(d.ControlTorque.x,Is.Zero);Assert.That(d.AerodynamicTorque.x,Is.Zero);
            Assert.That(d.AngularVelocity.x,Is.Zero);Assert.That(Quaternion.Angle(q,Quaternion.identity),Is.LessThan(.001f));
        }
        [Test] public void ReleasingPitchDampsRotationInsteadOfWindRestartingIt()
        {
            var d=new AcrobaticDynamics();var p=AcrobaticProfile.ForMass(1.1f);var q=Quaternion.identity;
            for(int i=0;i<120;i++)q=d.Step(q,Vector3.right,10,0,1f/120,p);
            float before=Mathf.Abs(d.AngularVelocity.x);
            for(int i=0;i<360;i++)q=d.Step(q,Vector3.zero,10,0,1f/120,p,new Vector3(0,5,8));
            Assert.That(Mathf.Abs(d.AngularVelocity.x),Is.LessThan(before*.001f));
        }
        [TestCase(-1f)][TestCase(1f)] public void FullPitchKeepsHelpfulFlowAndYawAlignment(float command)
        {
            var d=new AcrobaticDynamics();var p=AcrobaticProfile.ForMass(1.1f);var air=new Vector3(2,-3*command,8);float speed=air.magnitude;
            d.Step(Quaternion.identity,Vector3.right*command,speed,0,.01f,p,air);
            var expected=Vector3.Cross(Vector3.forward,air.normalized)*p.Torque.x*Mathf.Clamp(speed*speed/64,0,9)*2.5f;
            Assert.That(Vector3.Distance(d.AerodynamicTorque,expected),Is.LessThan(.00001f));
            d.Step(Quaternion.identity,Vector3.zero,speed,0,.01f,p,air);
            Assert.That(d.AerodynamicTorque.y,Is.EqualTo(expected.y).Within(.00001));
        }
        [TestCase(-1f)][TestCase(1f)] public void OpposingAirCannotReverseOrReduceIncreasingPitchCommands(float sign)
        {
            float previous=0;
            for(int i=0;i<=100;i++)
            {
                float command=sign*i/100f,control=command*.19f;
                float total=control+AcrobaticDynamics.PitchFlowTorque(-sign*2,command,control);
                Assert.That(total*sign,Is.GreaterThanOrEqualTo(previous-1e-6f));
                Assert.That(total*sign,Is.GreaterThanOrEqualTo(Mathf.Abs(control)*.5f-1e-6f));previous=total*sign;
            }
        }
    }
}
