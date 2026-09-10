using System;
using System.Collections.Generic;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Telemetry
{
    // Deterministic simple-environment replay. The adapter exposes original raw tracking;
    // this runner also restores accepted calibration AFTER the recorded step, matching
    // BirdFlightDriver. Caller supplies a fixed wind/environment when desired. Streamed
    // collision/world rebases and external tracking-origin changes are not reconstructed.
    public sealed class TelemetryReplay
    {
        private readonly TelemetryReplayInput recording;
        private readonly Gate gate;
        private double calibrationSequence;
        public BirdFlightController Controller { get; }
        public TelemetrySample Recorded => recording.Current;
        private sealed class Gate:IFlightInput
        {
            public FlightInputFrame Frame;
            public string Mode=>"Calibrated telemetry replay";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        public TelemetryReplay(string path,BirdFlightProfile profile=null,IWindField wind=null,IFlightEnvironment environment=null)
        {
            recording=new TelemetryReplayInput(path);gate=new Gate();
            Controller=new BirdFlightController(gate,recording.Header.spawn,profile:profile??recording.Header.flight,wind:wind,environment:environment);
        }
        public bool Step()
        {
            if(!recording.TryAdvance(out var dt))return false;
            var s=recording.Current;gate.Frame=recording.Sample(dt);
            if(recording.Header.input!=null && recording.Header.input.StartsWith("XR",StringComparison.Ordinal) && s.input_wings_enabled==0)
            {
                gate.Frame.LeftWing=FlightInputFrame.Neutral.LeftWing;gate.Frame.RightWing=FlightInputFrame.Neutral.RightWing;
                gate.Frame.Bank=gate.Frame.Tuck=gate.Frame.Flare=0;gate.Frame.BodyTracked=false;
            }
            Controller.StreamingBlocked=s.streaming_blocked>0;
            Controller.Step(dt);
            if(s.calibrated>0 && s.calibration_sequence!=calibrationSequence)
            {
                Controller.Calibrate(Neutral(s));calibrationSequence=s.calibration_sequence;
            }
            return true;
        }
        public static FlightInputFrame Neutral(TelemetrySample s)
        {
            var f=FlightInputFrame.Neutral;
            f.HeadPosition=new Vector3((float)s.calibration_head_origin_x,(float)s.calibration_head_origin_y,(float)s.calibration_head_origin_z);
            f.BodyOrientation=new Quaternion((float)s.calibration_heading_x,(float)s.calibration_heading_y,(float)s.calibration_heading_z,(float)s.calibration_heading_w);
            f.HeadOrientation=f.BodyOrientation*new Quaternion((float)s.calibration_pitch_x,(float)s.calibration_pitch_y,(float)s.calibration_pitch_z,(float)s.calibration_pitch_w);
            f.LookDirection=f.HeadOrientation*Vector3.forward;f.HeadTracked=true;f.BodyTracked=s.raw_body_tracked>0;
            f.LeftWing.Position=new Vector3((float)s.calibration_left_neutral_x,(float)s.calibration_left_neutral_y,(float)s.calibration_left_neutral_z);
            f.RightWing.Position=new Vector3((float)s.calibration_right_neutral_x,(float)s.calibration_right_neutral_y,(float)s.calibration_right_neutral_z);
            f.LeftWing.Orientation=new Quaternion((float)s.calibration_left_rotation_x,(float)s.calibration_left_rotation_y,(float)s.calibration_left_rotation_z,(float)s.calibration_left_rotation_w);
            f.RightWing.Orientation=new Quaternion((float)s.calibration_right_rotation_x,(float)s.calibration_right_rotation_y,(float)s.calibration_right_rotation_z,(float)s.calibration_right_rotation_w);
            return f;
        }
    }
}
