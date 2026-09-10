using System;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.Telemetry;

namespace VoarVR.Tests
{
    public sealed class TelemetryCalibrationReplayTests
    {
        private sealed class Input:IFlightInput
        {
            public FlightInputFrame Frame;
            public string Mode=>"Replay equivalence fixture";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        // Reflection is confined to fixture construction. The runtime writer has explicit
        // scalar writes and no reflection or allocation per captured frame.
        private static void Put(ref TelemetrySample sample,string name,double value)
        { object box=sample;typeof(TelemetrySample).GetField(name).SetValue(box,value);sample=(TelemetrySample)box; }
        private static void V(ref TelemetrySample sample,string name,Vector3 v)
        { Put(ref sample,name+"_x",v.x);Put(ref sample,name+"_y",v.y);Put(ref sample,name+"_z",v.z); }
        private static void Q(ref TelemetrySample sample,string name,Quaternion q)
        { V(ref sample,name,new Vector3(q.x,q.y,q.z));Put(ref sample,name+"_w",q.w); }
        [TestCase(false)][TestCase(true)] public void ReplayRestoresNonzeroHeadPitchAndAsymmetricNeutralAtAcceptedFrame(bool xrGate)
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".voartlm");
            try
            {
                var frame=FlightInputFrame.Neutral;frame.HeadTracked=frame.BodyTracked=true;
                frame.HeadPosition=new Vector3(.1f,1.7f,.2f);frame.BodyOrientation=Quaternion.Euler(0,32,0);
                frame.HeadOrientation=Quaternion.Euler(-9,32,0);frame.LookDirection=frame.HeadOrientation*Vector3.forward;
                frame.LeftWing.Position=frame.HeadPosition+frame.BodyOrientation*new Vector3(-.72f,-.19f,-.03f);
                frame.RightWing.Position=frame.HeadPosition+frame.BodyOrientation*new Vector3(.58f,-.14f,.02f);
                frame.LeftWing.Orientation=frame.RightWing.Orientation=Quaternion.Euler(17,32,0);
                var calibration=new BirdTrackingCalibration();Assert.That(calibration.Capture(frame),Is.True);
                var input=new Input { Frame=frame };var p=BirdFlightProfile.Duck();
                var direct=new BirdFlightController(input,Vector3.up*100,profile:p);
                var header=new FlightTelemetry.Header {session="calibration-test",input=xrGate?"XR / OpenXR":input.Mode,flight=p,spawn=Vector3.up*100};
                var writer=new TelemetryWriter(path,JsonUtility.ToJson(header),512);
                for(int i=0;i<240;i++)
                {
                    float dt=i%2==0?1f/90:1f/120;
                    if(i==120)
                    {
                        frame.HeadOrientation=Quaternion.Euler(6,32,0);frame.LookDirection=frame.HeadOrientation*Vector3.forward;
                        frame.LeftWing.Position+=frame.BodyOrientation*Vector3.left*.05f;
                        calibration.Capture(frame);
                    }
                    input.Frame=frame;
                    input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=frame.BodyOrientation*(Vector3.down*Mathf.Max(0,Mathf.Sin(i*.12f))*1.7f);
                    var raw=input.Frame;
                    bool wingsEnabled=!xrGate || i!=0 && i!=120;
                    if(!wingsEnabled)
                    {
                        input.Frame.LeftWing=FlightInputFrame.Neutral.LeftWing;input.Frame.RightWing=FlightInputFrame.Neutral.RightWing;
                        input.Frame.BodyTracked=false;
                    }
                    direct.Step(dt);if(i==0 || i==120)direct.Calibrate(frame); // Driver accepts after the sampled step.
                    var s=new TelemetrySample {dt=dt,calibrated=1,calibration_sequence=i<120?1:2,input_wings_enabled=wingsEnabled?1:0,raw_head_tracked=1,raw_body_tracked=1,raw_left_tracked=1,raw_right_tracked=1};
                    V(ref s,"raw_head_position",frame.HeadPosition);Q(ref s,"raw_head_rotation",frame.HeadOrientation);
                    V(ref s,"raw_look",frame.LookDirection);Q(ref s,"raw_body_rotation",frame.BodyOrientation);
                    V(ref s,"raw_left_position",frame.LeftWing.Position);V(ref s,"raw_right_position",frame.RightWing.Position);
                    V(ref s,"raw_left_velocity",raw.LeftWing.Velocity);V(ref s,"raw_right_velocity",raw.RightWing.Velocity);
                    Q(ref s,"raw_left_rotation",frame.LeftWing.Orientation);Q(ref s,"raw_right_rotation",frame.RightWing.Orientation);
                    V(ref s,"calibration_head_origin",calibration.HeadOrigin);Q(ref s,"calibration_heading",calibration.Heading);Q(ref s,"calibration_pitch",calibration.NeutralLookPitch);
                    V(ref s,"calibration_left_neutral",calibration.LeftNeutral);V(ref s,"calibration_right_neutral",calibration.RightNeutral);
                    Q(ref s,"calibration_left_rotation",calibration.LeftRotation);Q(ref s,"calibration_right_rotation",calibration.RightRotation);
                    Assert.That(writer.Capture(s),Is.True);
                }
                writer.Stop();Assert.That(SpinWait.SpinUntil(()=>writer.Finished,3000),Is.True);Assert.That(writer.Error,Is.Null);
                var replay=new TelemetryReplay(path);while(replay.Step()){}
                Assert.That(Vector3.Distance(replay.Controller.State.Position,direct.State.Position),Is.LessThan(.001f));
                Assert.That(Vector3.Distance(replay.Controller.State.Velocity,direct.State.Velocity),Is.LessThan(.001f));
                Assert.That(replay.Controller.HeadPitchInput,Is.EqualTo(direct.HeadPitchInput).Within(.0001f));
            }
            finally {if(File.Exists(path))File.Delete(path);}
        }
        [Test] public void AlulaDeployedBrakingDoesNotAddStillAirEnergy()
        {
            var p=Resources.Load<BirdCharacterDefinition>("Characters/Magpie").BuildProfile();p.InitialSpeedMps=4;
            var input=new Input {Frame=FlightInputFrame.Neutral};input.Frame.Flare=1;
            var c=new BirdFlightController(input,Vector3.up*100,profile:p);float energy=c.MechanicalEnergy;bool deployed=false;
            for(int i=0;i<600;i++)
            {
                c.Step(1f/120);
                deployed|=c.EffectiveStallAngleDeg>p.StallAngleDeg+.1f;
                Assert.That(c.MechanicalEnergy,Is.LessThanOrEqualTo(energy+.001f));energy=c.MechanicalEnergy;
            }
            Assert.That(deployed,Is.True,"Must actually exercise alula-modified stall behavior");
        }
    }
}
