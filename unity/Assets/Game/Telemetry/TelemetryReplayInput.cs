using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Telemetry
{
    // Offline/debug adapter: advance explicitly with the original recorded dt, then
    // Controller.Step(dt). It does not recreate streamed collisions or weather fields.
    public sealed class TelemetryReplayInput : IFlightInput
    {
        private readonly List<TelemetrySample> samples=new List<TelemetrySample>();
        private int index;
        private FlightInputFrame current;
        public string Mode => "Telemetry replay / raw tracking";
        public FlightTelemetry.Header Header { get; }
        public int Count => samples.Count;
        public TelemetrySample Current { get; private set; }
        public TelemetryReplayInput(string path)
        {
            using(var r=new BinaryReader(File.OpenRead(path),Encoding.UTF8))
            {
                if(Encoding.ASCII.GetString(r.ReadBytes(8))!="VOARTLM1")throw new InvalidDataException("Unsupported telemetry format");
                int version=r.ReadInt32();if(version!=1 && version!=2 && version!=3)throw new InvalidDataException("Unsupported telemetry schema");
                int size=r.ReadInt32(); if(size<=0 || size>1048576)throw new InvalidDataException("Invalid header size");
                Header=JsonUtility.FromJson<FlightTelemetry.Header>(Encoding.UTF8.GetString(r.ReadBytes(size)));
                bool current=Header.fields.SequenceEqual(TelemetrySample.Fields);
                var legacy=Header.fields.Select(name=>typeof(TelemetrySample).GetField(name)).ToArray();
                if(legacy.Any(f=>f==null) || version==3 && (!current || !Header.wideFields.SequenceEqual(TelemetrySample.WideFields)))throw new InvalidDataException("Incompatible frame schema");
                while(r.BaseStream.Position<r.BaseStream.Length)
                {
                    if(r.BaseStream.Length-r.BaseStream.Position<5)break;
                    byte kind=r.ReadByte();int length=r.ReadInt32();
                    if(length<0 || length>1048576)throw new InvalidDataException("Invalid record size");
                    if(r.BaseStream.Length-r.BaseStream.Position<length)break;
                    if(kind==1)
                    {
                        if(length!=(version==3?TelemetrySample.CompactSize:Header.fields.Length*8))throw new InvalidDataException("Invalid frame size");
                        if(version==3)samples.Add(TelemetrySample.ReadCompact(r));
                        else if(current)samples.Add(TelemetrySample.Read(r));
                        else {object value=new TelemetrySample();foreach(var f in legacy)f.SetValue(value,r.ReadDouble());samples.Add((TelemetrySample)value);}
                    }
                    else r.BaseStream.Seek(length,SeekOrigin.Current);
                }
            }
        }
        public bool TryAdvance(out float dt)
        {
            dt=0;if(index>=samples.Count)return false;
            Current=samples[index++];dt=(float)Current.dt;current=Raw(Current);return true;
        }
        public FlightInputFrame Sample(float deltaTime)=>current;
        public void Rewind(){index=0;current=FlightInputFrame.Neutral;}
        public static FlightInputFrame Raw(TelemetrySample s) => new FlightInputFrame
        {
            ControlModePressed=s.raw_control_mode_pressed>0,
            LeftWing=new WingInput { Position=new Vector3((float)s.raw_left_position_x,(float)s.raw_left_position_y,(float)s.raw_left_position_z), Velocity=new Vector3((float)s.raw_left_velocity_x,(float)s.raw_left_velocity_y,(float)s.raw_left_velocity_z), Orientation=new Quaternion((float)s.raw_left_rotation_x,(float)s.raw_left_rotation_y,(float)s.raw_left_rotation_z,(float)s.raw_left_rotation_w), Tracked=s.raw_left_tracked>0 },
            RightWing=new WingInput { Position=new Vector3((float)s.raw_right_position_x,(float)s.raw_right_position_y,(float)s.raw_right_position_z), Velocity=new Vector3((float)s.raw_right_velocity_x,(float)s.raw_right_velocity_y,(float)s.raw_right_velocity_z), Orientation=new Quaternion((float)s.raw_right_rotation_x,(float)s.raw_right_rotation_y,(float)s.raw_right_rotation_z,(float)s.raw_right_rotation_w), Tracked=s.raw_right_tracked>0 },
            HeadPosition=new Vector3((float)s.raw_head_position_x,(float)s.raw_head_position_y,(float)s.raw_head_position_z),
            HeadOrientation=new Quaternion((float)s.raw_head_rotation_x,(float)s.raw_head_rotation_y,(float)s.raw_head_rotation_z,(float)s.raw_head_rotation_w),
            LookDirection=new Vector3((float)s.raw_look_x,(float)s.raw_look_y,(float)s.raw_look_z),
            BodyOrientation=new Quaternion((float)s.raw_body_rotation_x,(float)s.raw_body_rotation_y,(float)s.raw_body_rotation_z,(float)s.raw_body_rotation_w),
            GroundMove=new Vector2((float)s.raw_ground_move_x,(float)s.raw_ground_move_y),
            HeadTracked=s.raw_head_tracked>0,
            BodyTracked=s.raw_body_tracked>0,
            Bank=(float)s.raw_bank,
            Tuck=(float)s.raw_tuck,
            Flare=(float)s.raw_flare,
            ResetPressed=s.raw_reset>0,
            RecalibratePressed=s.raw_recalibrate>0,
            CharacterSelectPressed=s.raw_characterselect>0,
            PausePressed=s.raw_pause>0,
            ViewTogglePressed=s.raw_viewtoggle>0,
            WindModePressed=s.raw_windmode>0,
            HudTogglePressed=s.raw_hudtoggle>0,
            MarkerPressed=s.raw_marker>0,
            ButtonsHeld=(uint)s.raw_buttons
        };
    }
}
