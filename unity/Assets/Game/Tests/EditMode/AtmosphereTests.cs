using NUnit.Framework;
using UnityEngine;
using VoarVR.World;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class AtmosphereTests
    {
        private sealed class CirclingGlide : IFlightInput
        {
            public string Mode => "Unpowered circle";
            public FlightInputFrame Sample(float dt)
            {
                var frame=FlightInputFrame.Neutral;frame.Bank=.55f;return frame;
            }
        }
        [Test] public void DragonCanClimbAnActualFinitePlumeWithoutWingbeats()
        {
            var go=new GameObject("Finite plume soaring");
            try
            {
                var wind=go.AddComponent<WindField>();
                var plume=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);
                var core=plume.Center(0,plume.Altitude);
                var spawn=new Vector3((float)core.X-8,(float)core.Y,(float)core.Z);
                var profile=Resources.Load<BirdCharacterDefinition>("Characters/Dragon").BuildProfile();
                var soaring=new BirdFlightController(new CirclingGlide(),spawn,profile:profile,wind:wind);
                var calm=new BirdFlightController(new CirclingGlide(),spawn,profile:profile);
                for(int i=0;i<2400;i++) { soaring.Step(1f/120);calm.Step(1f/120); }
                Assert.That(soaring.State.Position.y,Is.GreaterThan(spawn.y+3));
                Assert.That(soaring.State.Position.y,Is.GreaterThan(calm.State.Position.y+10));
                Assert.That(soaring.StrokeForce.sqrMagnitude,Is.LessThan(.0001));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void FeaturesHaveDeterministicFiniteSmoothLifetimes()
        {
            var a=AtmosphereModel.GetPlume(7319,-12,81,4,WindMode.Assisted);
            var b=AtmosphereModel.GetPlume(7319,-12,81,4,WindMode.Assisted);
            Assert.That(a.X,Is.EqualTo(b.X));Assert.That(a.Z,Is.EqualTo(b.Z));
            Assert.That(a.Envelope(a.Birth),Is.Zero);
            Assert.That(a.Envelope(a.Birth+80),Is.EqualTo(1).Within(.00001));
            Assert.That(a.Envelope(a.Birth+160),Is.Zero);
            Assert.That(a.Envelope(a.Birth-1),Is.Zero);
            var early=a.Center(a.Birth+20,a.Altitude);
            var late=a.Center(a.Birth+100,a.Altitude+30);
            Assert.That(late.X-early.X,Is.GreaterThan(20));
        }
        [Test] public void AirIsRepeatableAndContinuousAcrossCellsAndEpochs()
        {
            foreach(double x in new[]{-384.0,-192,0,192,384})
            {
                var a=AtmosphereModel.SampleLogical(x-.001,50,24,80,7319,WindMode.Assisted);
                var b=AtmosphereModel.SampleLogical(x+.001,50,24,80,7319,WindMode.Assisted);
                Assert.That(Vector3.Distance(a,b),Is.LessThan(.01));
                Assert.That(a,Is.EqualTo(AtmosphereModel.SampleLogical(x-.001,50,24,80,7319,WindMode.Assisted)));
                var before=AtmosphereModel.SampleLogical(x,50,24,79.999,7319,WindMode.Assisted);
                var after=AtmosphereModel.SampleLogical(x,50,24,80.001,7319,WindMode.Assisted);
                Assert.That(Vector3.Distance(before,after),Is.LessThan(.01));
            }
        }
        [Test] public void PlumesProvideUsefulLiftButNotInfiniteElevators()
        {
            var plume=AtmosphereModel.GetPlume(7319,0,0,0,WindMode.Assisted);
            var center=plume.Center(80,plume.Altitude);
            var core=AtmosphereModel.SampleLogical(center.X,center.Y,center.Z,80,7319,WindMode.Assisted);
            var high=AtmosphereModel.SampleLogical(center.X,900,center.Z,80,7319,WindMode.Assisted);
            Assert.That(core.y,Is.GreaterThan(5));
            Assert.That(Mathf.Abs(high.y),Is.LessThan(.01));
            Assert.That(AtmosphereModel.SampleLogical(center.X,center.Y,center.Z,80,7319,WindMode.StillAir),Is.EqualTo(Vector3.zero));
        }
        [Test] public void WildContainsSinkingFeaturesAndAltitudeLayersChangeHeading()
        {
            bool found=false;
            for(int i=0;i<20;i++)
            {
                var plume=AtmosphereModel.GetPlume(7319,i,0,0,WindMode.Wild);
                if(plume.Strength>=0) continue;
                var c=plume.Center(80,plume.Altitude);
                if(AtmosphereModel.SampleLogical(c.X,c.Y,c.Z,80,7319,WindMode.Wild).y < -2) found=true;
            }
            Assert.That(found,Is.True);
            var low=AtmosphereModel.SampleLogical(21,250,12,10,7319,WindMode.Touring);
            var high=AtmosphereModel.SampleLogical(21,500,12,10,7319,WindMode.Touring);
            Assert.That(Vector3.Distance(low,high),Is.GreaterThan(.1));
        }
        [Test] public void FloatingOriginPreservesExactFieldSample()
        {
            var go=new GameObject("Atmosphere rebase test");
            try
            {
                var space=go.AddComponent<WorldSpace>();var field=go.AddComponent<WindField>();field.Configure(space);
                var local=new Vector3(600,60,-350);
                var expected=field.Sample(local,37);
                var delta=new Vector3(512,0,-256);
                space.Shift(delta);
                Assert.That(field.Sample(local-delta,37),Is.EqualTo(expected));
                space.Shift(new Vector3(10000000,0,-10000000));
                Assert.That(float.IsNaN(field.Sample(Vector3.up*70,37).y),Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
