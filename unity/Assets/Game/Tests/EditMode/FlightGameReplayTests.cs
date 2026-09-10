using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Telemetry;
namespace VoarVR.Tests
{
    public sealed class FlightGameReplayTests
    {
        [TestCase(1)][TestCase(2)] public void LegacyAndCurrentContainersRestoreInputWithoutInventingAdvancedMode(int version)
        {
            var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                var header=new FlightTelemetry.Header {schemaVersion=version,flight=BirdFlightProfile.Duck(),spawn=Vector3.up*100};
                if(version==1)header.fields=TelemetrySample.Fields.Take(253).ToArray();
                var sample=new TelemetrySample {dt=1d/90,raw_bank=.4,raw_look_z=1,raw_left_rotation_w=1,raw_right_rotation_w=1,raw_left_tracked=1,raw_right_tracked=1,control_mode=version==2?1:0};
                using(var w=new BinaryWriter(File.Create(path)))
                {w.Write(Encoding.ASCII.GetBytes("VOARTLM1"));w.Write(version);var json=Encoding.UTF8.GetBytes(JsonUtility.ToJson(header));w.Write(json.Length);w.Write(json);w.Write((byte)1);w.Write(header.fields.Length*8);foreach(var field in header.fields)w.Write((double)typeof(TelemetrySample).GetField(field).GetValue(sample));}
                var input=new TelemetryReplayInput(path);Assert.That(input.TryAdvance(out var dt),Is.True);Assert.That(dt,Is.EqualTo(1f/90).Within(.00001));Assert.That(input.Sample(dt).Bank,Is.EqualTo(.4f));
                var replay=new TelemetryReplay(path);Assert.That(replay.Step(),Is.True);Assert.That(replay.Controller.ControlMode,Is.EqualTo(version==2?FlightControlMode.Acrobatic:FlightControlMode.Beginner));
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }
        [Test] public void CustomAdvancedProfileSurvivesReset()
        {
            var c=new BirdFlightController(new VoarVR.Input.SyntheticFlightInput(),Vector3.up*100);
            var profile=AcrobaticProfile.ForMass(1);profile.MaxRateDeg=63;c.ConfigureAdvanced(profile);c.Reset();
            Assert.That(c.AdvancedProfile.MaxRateDeg,Is.EqualTo(63));
        }
    }
}
