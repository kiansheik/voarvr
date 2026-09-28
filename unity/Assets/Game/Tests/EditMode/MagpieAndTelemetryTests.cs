using System;
using System.IO;
using System.Linq;
using System.Text;
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
                var s=new TelemetrySample {dt=.013,raw_left_position_x=-.71,raw_left_rotation_w=1,raw_right_rotation_w=1,raw_head_rotation_w=1,raw_body_rotation_w=1,raw_head_tracked=1,raw_marker=1,
                    raw_ground_turn=.75,mapped_ground_turn=-.25,raw_left_motion_estimated=1,
                    raw_right_motion_estimated=0,mapped_left_motion_estimated=0,mapped_right_motion_estimated=1};
                Assert.That(writer.Capture(s),Is.True);
                writer.Event(TelemetryEvent.Marker,1,4);writer.Stop();
                Assert.That(SpinWait.SpinUntil(()=>writer.Finished,3000),Is.True);
                Assert.That(writer.Error,Is.Null);
                var replay=new TelemetryReplayInput(path);
                Assert.That(replay.Header.schemaVersion,Is.EqualTo(4));
                Assert.That(replay.Count,Is.EqualTo(1));Assert.That(replay.TryAdvance(out float dt),Is.True);
                Assert.That(dt,Is.EqualTo(.013f));Assert.That(replay.Sample(dt).LeftWing.Position.x,Is.EqualTo(-.71f));
                Assert.That(replay.Sample(dt).GroundTurn,Is.EqualTo(.75f));
                Assert.That(replay.Sample(dt).LeftWing.MotionEstimated,Is.True);
                Assert.That(replay.Sample(dt).RightWing.MotionEstimated,Is.False);
                Assert.That(replay.Current.mapped_ground_turn,Is.EqualTo(-.25));
                Assert.That(replay.Current.mapped_left_motion_estimated,Is.Zero);
                Assert.That(replay.Current.mapped_right_motion_estimated,Is.EqualTo(1));
                Assert.That(replay.Sample(dt).MarkerPressed,Is.True);Assert.That(replay.TryAdvance(out _),Is.False);
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }

        [TestCase(1,253)][TestCase(2,275)][TestCase(3,295)]
        public void LegacyBinarySchemasRetainValuesAndDefaultAbsentMotionInputs(int version,int fieldCount)
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                string[] fields=TelemetrySample.Fields.Take(fieldCount).ToArray();
                var header=new FlightTelemetry.Header {schemaVersion=version,fields=fields,session="legacy"};
                var bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(header));
                var sample=new TelemetrySample {timestamp=987654321.125,dt=.02,raw_left_tracked=1,
                    raw_left_velocity_y=-2.5,raw_ground_turn=.75,raw_left_motion_estimated=1};
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(Encoding.ASCII.GetBytes("VOARTLM1"));writer.Write(version);
                    writer.Write(bytes.Length);writer.Write(bytes);
                    writer.Write((byte)1);writer.Write(version==3?1200:fieldCount*8);
                    // Build the historical format independently of today's sample writer.
                    foreach(string name in fields)
                    {
                        double value=(double)typeof(TelemetrySample).GetField(name).GetValue(sample);
                        if(version<3||TelemetrySample.WideFields.Contains(name))writer.Write(value);
                        else writer.Write((float)value);
                    }
                    writer.Write((byte)3);writer.Write(4);writer.Write(0);
                }
                var replay=new TelemetryReplayInput(path);
                Assert.That(replay.TryAdvance(out float dt),Is.True);
                Assert.That(dt,Is.EqualTo(.02f));
                Assert.That(replay.Current.timestamp,Is.EqualTo(987654321.125));
                Assert.That(replay.Sample(dt).LeftWing.Velocity.y,Is.EqualTo(-2.5f));
                Assert.That(replay.Sample(dt).GroundTurn,Is.Zero);
                Assert.That(replay.Sample(dt).LeftWing.MotionEstimated,Is.False);
                Assert.That(replay.Sample(dt).RightWing.MotionEstimated,Is.False);
                Assert.That(replay.Current.mapped_ground_turn,Is.Zero);
                Assert.That(replay.Current.mapped_left_motion_estimated,Is.Zero);
                Assert.That(replay.Current.mapped_right_motion_estimated,Is.Zero);
                Assert.That(replay.TryAdvance(out _),Is.False,"Legacy frames must not consume the following record.");
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }

        [Test]
        public void CompactV4BinaryMatchesHeaderOrderAndPreservesWideCoordinates()
        {
            object boxed=new TelemetrySample();
            for(int i=0;i<TelemetrySample.Fields.Length;i++)
                typeof(TelemetrySample).GetField(TelemetrySample.Fields[i]).SetValue(boxed,i+.125);
            var sample=(TelemetrySample)boxed;sample.logical_x=5000000000.125;sample.timestamp=987654321.125;
            using(var stream=new MemoryStream())
            using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                sample.WriteCompact(writer);writer.Flush();
                Assert.That(TelemetrySample.Fields.Length,Is.EqualTo(301));
                Assert.That(stream.Length,Is.EqualTo(1224));
                stream.Position=0;
                using(var reader=new BinaryReader(stream,Encoding.UTF8,true))
                {
                    foreach(string name in TelemetrySample.Fields)
                    {
                        double expected=(double)typeof(TelemetrySample).GetField(name).GetValue(sample);
                        double actual=TelemetrySample.WideFields.Contains(name)?reader.ReadDouble():reader.ReadSingle();
                        Assert.That(actual,Is.EqualTo(expected),name);
                    }
                    stream.Position=0;
                    var restored=TelemetrySample.ReadCompact(reader);
                    foreach(string name in TelemetrySample.Fields)
                        Assert.That(typeof(TelemetrySample).GetField(name).GetValue(restored),
                            Is.EqualTo(typeof(TelemetrySample).GetField(name).GetValue(sample)),name);
                }
            }
        }

        [Test]
        public void VersionFourReplayPreservesSupportedTurningTrajectory()
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                var profile=BirdFlightProfile.Duck();var environment=new PlaneFlightEnvironment(0);
                var spawn=Vector3.up*(profile.CollisionRadius+FlightContactSolver.Skin);
                var sample=NeutralMotionSample();sample.raw_ground_turn=.75;sample.raw_ground_move_y=.5;
                var input=new RecordedFixtureInput {Frame=TelemetryReplayInput.Raw(sample)};
                var direct=new BirdFlightController(input,spawn,profile:profile,environment:environment);
                Assert.That(direct.TryRecoverToPerch(spawn,0),Is.True);
                var writer=new TelemetryWriter(path,JsonUtility.ToJson(new FlightTelemetry.Header
                    {session="ground-turn",spawn=spawn,flight=profile}),64);
                for(int i=0;i<45;i++){direct.Step((float)sample.dt);Assert.That(writer.Capture(sample),Is.True);}
                writer.Stop();Assert.That(SpinWait.SpinUntil(()=>writer.Finished,3000),Is.True);Assert.That(writer.Error,Is.Null);
                var replay=new TelemetryReplay(path,environment:environment);
                Assert.That(replay.Controller.TryRecoverToPerch(spawn,0),Is.True);
                while(replay.Step()){}
                Assert.That(Quaternion.Angle(direct.State.Rotation,Quaternion.identity),Is.GreaterThan(15));
                Assert.That(Quaternion.Angle(replay.Controller.State.Rotation,direct.State.Rotation),Is.LessThan(.01));
                Assert.That(Vector3.Distance(replay.Controller.State.Position,direct.State.Position),Is.LessThan(.001));
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }

        [Test]
        public void VersionFourReplayDoesNotTurnEstimatedDownstrokeIntoActiveForce()
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                var sample=NeutralMotionSample();
                sample.raw_left_velocity_y=sample.raw_right_velocity_y=-2.5;
                sample.raw_left_motion_estimated=sample.raw_right_motion_estimated=1;
                var writer=new TelemetryWriter(path,JsonUtility.ToJson(new FlightTelemetry.Header
                    {session="estimated-motion",spawn=Vector3.up*20,flight=BirdFlightProfile.Duck()}),8);
                Assert.That(writer.Capture(sample),Is.True);writer.Stop();
                Assert.That(SpinWait.SpinUntil(()=>writer.Finished,3000),Is.True);Assert.That(writer.Error,Is.Null);
                var replay=new TelemetryReplay(path);Assert.That(replay.Step(),Is.True);
                Assert.That(replay.Controller.LastInput.LeftWing.MotionEstimated,Is.True);
                Assert.That(replay.Controller.LastInput.RightWing.MotionEstimated,Is.True);
                Assert.That(replay.Controller.StrokeForce.sqrMagnitude,Is.LessThan(1e-6));
                sample.raw_left_motion_estimated=sample.raw_right_motion_estimated=0;
                var direct=new BirdFlightController(new RecordedFixtureInput {Frame=TelemetryReplayInput.Raw(sample)},Vector3.up*20);
                direct.Step((float)sample.dt);
                Assert.That(direct.StrokeForce.sqrMagnitude,Is.GreaterThan(1));
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }

        private sealed class RecordedFixtureInput:IFlightInput
        {
            public FlightInputFrame Frame;
            public string Mode=>"Telemetry motion fixture";
            public FlightInputFrame Sample(float dt)=>Frame;
        }

        private static TelemetrySample NeutralMotionSample()=>new TelemetrySample
        {
            dt=1f/90f,raw_left_tracked=1,raw_right_tracked=1,raw_left_position_x=-.65,
            raw_right_position_x=.65,raw_left_rotation_w=1,raw_right_rotation_w=1,
            raw_head_rotation_w=1,raw_body_rotation_w=1,raw_look_z=1,input_wings_enabled=1
        };
    }
}
