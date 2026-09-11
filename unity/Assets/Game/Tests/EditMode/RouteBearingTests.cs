using NUnit.Framework;
using UnityEngine;
using VoarVR.World;

namespace VoarVR.Tests
{
    public class RouteBearingTests
    {
        [Test]
        public void VisibleTargetsAndArrivalDoNotCoverTheViewWithATurnCue()
        {
            Assert.That(RouteBearing.TryProject(new Vector3(2,1,100),90,1,out _,out _),Is.False);
            Assert.That(RouteBearing.TryProject(new Vector3(0,0,-2),90,1,out _,out _),Is.False);
            Assert.That(RouteBearing.TryProject(new Vector3(float.NaN,0,10),90,1,out _,out _),Is.False);
        }

        [TestCase(100,0,-100,1,0)]
        [TestCase(-100,0,-100,-1,0)]
        [TestCase(0,100,0,0,1)]
        [TestCase(0,-100,0,0,-1)]
        [TestCase(0,0,-100,1,0)]
        public void OffscreenTargetsAlwaysGiveAFiniteTurnBearing(float x,float y,float z,int horizontal,int vertical)
        {
            Assert.That(RouteBearing.TryProject(new Vector3(x,y,z),90,1,out var cue,out float angle),Is.True);
            Assert.That(cue.z,Is.EqualTo(4));Assert.That(float.IsFinite(angle),Is.True);
            Assert.That(Mathf.Abs(cue.x),Is.LessThanOrEqualTo(2.601f));
            Assert.That(Mathf.Abs(cue.y),Is.LessThanOrEqualTo(1.921f));
            if(horizontal!=0)Assert.That(Mathf.Sign(cue.x),Is.EqualTo(horizontal));
            if(vertical!=0)Assert.That(Mathf.Sign(cue.y),Is.EqualTo(vertical));
            var arrow=Quaternion.Euler(0,0,angle)*Vector3.right;
            Assert.That(Vector2.Dot(new Vector2(arrow.x,arrow.y),new Vector2(cue.x,cue.y).normalized),Is.GreaterThan(.999f));
        }

        [Test]
        public void TurningAndTranslatingTheHeadReacquiresTheSameWorldTarget()
        {
            var player=new Vector3(1047,318,-483);var seed=new Vector3(140,245,200);
            var body=Quaternion.LookRotation(player-new Vector3(100,230,154));
            var eye=player+body*new Vector3(.24f,.3f,.4f);
            Assert.That(RouteBearing.TryProject(Quaternion.Inverse(body)*(seed-eye),90,1,out _,out _),Is.True);
            var looked=Quaternion.LookRotation(seed-eye);
            Assert.That(RouteBearing.TryProject(Quaternion.Inverse(looked)*(seed-eye),90,1,out _,out _),Is.False);
            var shift=new Vector3(1024,0,-1024);
            Assert.That(RouteBearing.TryProject(Quaternion.Inverse(looked)*((seed-shift)-(eye-shift)),90,1,out _,out _),Is.False);
        }
    }
}
