using NUnit.Framework;
using UnityEngine;
using VoarVR.Core;
using VoarVR.Flight;
using VoarVR.Input;
namespace VoarVR.Tests
{
    public class QuietCockpitTests
    {
        sealed class Input:IFlightInput {public FlightInputFrame Frame;public string Mode=>"fixture";public FlightInputFrame Sample(float dt)=>Frame;}
        [TestCase(0f)][TestCase(90f)][TestCase(-90f)]
        public void ComfortableGripDefinesPitchZeroAndBodyAxis(float grip)
        {
            var f=FlightInputFrame.Neutral;f.HeadTracked=f.BodyTracked=true;
            f.BodyOrientation=Quaternion.Euler(0,65,0);f.HeadOrientation=f.BodyOrientation;
            f.LeftWing.Position=f.BodyOrientation*f.LeftWing.Position;f.RightWing.Position=f.BodyOrientation*f.RightWing.Position;
            var left=Quaternion.Euler(grip,0,-90);var right=Quaternion.Euler(grip,0,90);
            f.LeftWing.Orientation=f.BodyOrientation*left;f.RightWing.Orientation=f.BodyOrientation*right;
            var input=new Input{Frame=f};var c=new BirdFlightController(input,Vector3.up*100);
            c.Calibrate(f);c.SetControlMode(FlightControlMode.Acrobatic);c.Step(.02f);
            Assert.That(c.Acrobatic.ControlTorque.x,Is.EqualTo(0).Within(.00001));
            f.LeftWing.Orientation=f.BodyOrientation*Quaternion.Euler(-16,0,0)*left;
            f.RightWing.Orientation=f.BodyOrientation*Quaternion.Euler(-16,0,0)*right;input.Frame=f;c.Step(.02f);
            Assert.That(c.Acrobatic.ControlTorque.x,Is.LessThan(-.02f),"Both comfortable grips must command the same body pitch direction");
            Assert.That(AcrobaticDynamics.CalibratedPitch(Quaternion.Euler(-16,0,0)*left,left),Is.EqualTo(-16).Within(.001));
            c.Calibrate(f);c.Step(.02f);Assert.That(c.Acrobatic.ControlTorque.x,Is.EqualTo(0).Within(.00001),"Recalibration replaces the old zero");
        }
        [TestCase(90,0)][TestCase(180,0)][TestCase(0,180)][TestCase(270,90)]
        public void AdvancedEyeBasisFollowsFullBodyWhileBeginnerRetainsHeading(float pitch,float roll)
        {
            var body=Quaternion.Euler(pitch,23,roll);var heading=Quaternion.Euler(0,23,0);
            Assert.That(Quaternion.Angle(FlightCamera.FirstPersonBasis(FlightControlMode.Acrobatic,body,heading),body),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(FlightCamera.FirstPersonBasis(FlightControlMode.Beginner,body,heading),heading),Is.LessThan(.001f));
        }
    }
}
