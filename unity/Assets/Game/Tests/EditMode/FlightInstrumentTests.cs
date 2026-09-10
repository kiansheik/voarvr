using NUnit.Framework;
using UnityEngine;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class FlightInstrumentTests
    {
        [Test] public void KnotsMeasureMotionThroughAirRatherThanGroundSpeed()
        {
            Assert.That(FlightHud.AirspeedKnots(Vector3.forward*10,Vector3.forward*4),Is.EqualTo(11.663067f).Within(.0001));
            Assert.That(FlightHud.AirspeedKnots(Vector3.forward*4,Vector3.forward*4),Is.Zero);
            Assert.That(FlightHud.AirspeedKnots(Vector3.forward*10,Vector3.back*4),Is.GreaterThan(27));
        }
        [Test] public void LiftCueDoesNotClaimClimbingJustBecauseAirIsRising()
        {
            Assert.That(FlightHud.LiftCue(3,-1),Does.Contain("RISING AIR"));
            Assert.That(FlightHud.LiftCue(3,-1),Does.Not.Contain("LIFT FOUND"));
            Assert.That(FlightHud.LiftCue(3,1),Does.Contain("GLIDE + CIRCLE"));
            Assert.That(FlightHud.LiftCue(-2,1),Does.Contain("SINKING AIR"));
            Assert.That(FlightHud.AirColor(3),Is.EqualTo(FlightHud.LiftColor));
            Assert.That(FlightHud.AirColor(-2),Is.EqualTo(FlightHud.SinkColor));
        }
        [Test] public void AssistedOuterPlumeHasUsefulLiftAndStillAirRemainsEmpty()
        {
            var p=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);var core=p.Center(0,p.Altitude);
            var outer=AtmosphereModel.SampleLogical(core.X+22,core.Y,core.Z,0,7319,WindMode.Assisted);
            Assert.That(outer.y,Is.GreaterThan(4),"Useful lift must extend outside a tiny exact core");
            Assert.That(AtmosphereModel.SampleLogical(core.X,core.Y,core.Z,0,7319,WindMode.StillAir),Is.EqualTo(Vector3.zero));
        }
    }
}
