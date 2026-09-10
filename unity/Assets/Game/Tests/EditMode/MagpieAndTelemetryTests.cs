using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.Telemetry;

namespace VoarVR.Tests
{
    public sealed class MagpieAndTelemetryTests
    {
        [Test] public void MorphologyUsesBothWingsAndDerivedSIValues()
        {
            var m=new BirdMorphology();
            Assert.That(m.AspectRatio,Is.EqualTo(.56f*.56f/.06171f).Within(.0001f));
            Assert.That(m.WingLoading,Is.EqualTo(.2175f*9.81f/.06171f).Within(.0001f));
            Assert.That(m.IsValid,Is.True);m.BothWingAreaM2=0;Assert.That(m.IsValid,Is.False);
        }
        [Test] public void FeatherFanIsMonotonicAndProgressive()
        {
            for(int feather=0;feather<10;feather++)
            {
                float previous=-1;
                for(int i=0;i<=100;i++)
                {
                    float angle=AvianWingPresentation.CoupledFanAngle(feather,10,i*.01f,38);
                    Assert.That(angle,Is.InRange(previous,38f));previous=angle;
                }
            }
            Assert.That(AvianWingPresentation.CoupledFanAngle(9,10,1,38),Is.GreaterThan(AvianWingPresentation.CoupledFanAngle(0,10,1,38)*5));
        }
        [Test] public void AlulaOnlyDeploysForSlowPositiveIncidenceOrBraking()
        {
            Assert.That(AvianWingPresentation.AlulaDeployment(12,24,1,0),Is.Zero);
            Assert.That(AvianWingPresentation.AlulaDeployment(3,-15,0,0),Is.Zero);
            Assert.That(AvianWingPresentation.AlulaDeployment(3,24,1,1),Is.Zero);
            Assert.That(AvianWingPresentation.AlulaDeployment(3,24,0,0),Is.EqualTo(1));
            Assert.That(AvianWingPresentation.TailSpread(1,4,0),Is.GreaterThan(AvianWingPresentation.TailSpread(0,8,0)));
        }
        [TestCase("Duck")][TestCase("Dragon")][TestCase("Magpie")]
        public void PassiveStillAirDoesNotGainMechanicalEnergy(string species)
        {
            var p=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
            var input=new SyntheticFlightInput();var c=new BirdFlightController(input,Vector3.up*100,profile:p);
            float initial=c.MechanicalEnergy;
            for(int i=0;i<1200;i++)c.Step(1f/120);
            Assert.That(c.MechanicalEnergy,Is.LessThan(initial));
            Assert.That(c.State.Position.y,Is.LessThan(100));
        }
        [Test] public void MagpieModerateHumanCadenceCanClimb()
        {
            var p=Resources.Load<BirdCharacterDefinition>("Characters/Magpie").BuildProfile();
            var input=new SyntheticFlightInput(SyntheticGesture.Flap);
            var c=new BirdFlightController(input,Vector3.up*100,profile:p);
            for(int i=0;i<1200;i++)c.Step(1f/120);
            TestContext.WriteLine("Magpie1Hz .3m strokes final="+c.State.Position+" speed="+c.State.Speed);
            Assert.That(c.State.Position.y,Is.GreaterThan(100));
        }
        [Test] public void FullRingDropsNewFramesWithoutCorruptingWrittenRecords()
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                var writer=new TelemetryWriter(path,JsonUtility.ToJson(new FlightTelemetry.Header { session="pressure" }),2);
                int accepted=0;
                for(int i=0;i<5000;i++)if(writer.Capture(new TelemetrySample {frame=i,dt=.01}))accepted++;
                writer.Stop();Assert.That(SpinWait.SpinUntil(()=>writer.Finished,3000),Is.True);Assert.That(writer.Error,Is.Null);
                Assert.That(writer.Dropped,Is.GreaterThan(0));Assert.That(accepted+writer.Dropped,Is.EqualTo(5000));
                var replay=new TelemetryReplayInput(path);Assert.That(replay.Count,Is.EqualTo(accepted));
                double previous=-1;
                while(replay.TryAdvance(out _)){Assert.That(replay.Current.frame,Is.GreaterThan(previous));previous=replay.Current.frame;}
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }
        [Test] public void BinaryWriterAndReplayPreserveRawInputAndOriginalDt()
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                var writer=new TelemetryWriter(path,JsonUtility.ToJson(new FlightTelemetry.Header { session="unit",flight=new BirdFlightProfile() }),8);
                var s=new TelemetrySample {dt=.013,raw_left_position_x=-.71,raw_left_rotation_w=1,raw_right_rotation_w=1,raw_head_rotation_w=1,raw_body_rotation_w=1,raw_head_tracked=1,raw_marker=1};
                Assert.That(writer.Capture(s),Is.True);
                writer.Event(TelemetryEvent.Marker,1,4);writer.Stop();
                Assert.That(SpinWait.SpinUntil(()=>writer.Finished,3000),Is.True);
                Assert.That(writer.Error,Is.Null);
                var replay=new TelemetryReplayInput(path);
                Assert.That(replay.Count,Is.EqualTo(1));Assert.That(replay.TryAdvance(out float dt),Is.True);
                Assert.That(dt,Is.EqualTo(.013f));Assert.That(replay.Sample(dt).LeftWing.Position.x,Is.EqualTo(-.71f));
                Assert.That(replay.Sample(dt).MarkerPressed,Is.True);Assert.That(replay.TryAdvance(out _),Is.False);
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }
    }
}
