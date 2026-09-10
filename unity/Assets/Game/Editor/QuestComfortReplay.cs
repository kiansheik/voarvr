using System;
using System.Reflection;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Telemetry;
namespace VoarVR.Editor
{
    public static class QuestComfortReplay
    {
        private sealed class RecordedAir:IWindField,IWindAssistance
        {public Vector3 Value;public bool AutomaticFeathering=>true;public string ModeName=>"Recorded Assisted air";public Vector3 Sample(Vector3 p,float time)=>Value;}
        public static string Run(string path)
        {
            var input=new TelemetryReplayInput(path);var air=new RecordedAir();BirdFlightController c=null;double start=-1;float elapsed=0,stalled=0;double firstY=0,lastRecordedY=0;
            while(input.TryAdvance(out float dt))
            {
                var r=input.Current;if(start<0)start=r.timestamp;double t=r.timestamp-start;if(t<38.98)continue;if(t>50.53)break;
                if(c==null)
                {
                    c=new BirdFlightController(input,Vector3.zero,profile:input.Header.flight,wind:air);c.Calibrate(TelemetryReplay.Neutral(r));
                    var q=new Quaternion((float)r.rotation_x,(float)r.rotation_y,(float)r.rotation_z,(float)r.rotation_w);
                    var state=new BirdState{Position=new Vector3((float)r.position_x,(float)r.position_y,(float)r.position_z),Velocity=new Vector3((float)r.velocity_x,(float)r.velocity_y,(float)r.velocity_z),Rotation=q,Phase=FlightPhase.Gliding};
                    typeof(BirdFlightController).GetProperty("State").SetValue(c,state);
                    var e=q.eulerAngles;Set(c,"pitchDeg",Mathf.DeltaAngle(0,e.x));Set(c,"yawDeg",e.y);Set(c,"rollDeg",Mathf.DeltaAngle(0,e.z));Set(c,"simulationTime",(float)r.simulation_time);firstY=r.position_y;
                    continue; // Recorded state is already post-step.
                }
                air.Value=new Vector3((float)r.wind_x,(float)r.wind_y,(float)r.wind_z);c.Step(dt);elapsed+=dt;if(c.IsStalled)stalled+=dt;lastRecordedY=r.position_y;
            }
            return "Prescribed recorded-air counterfactual (not spatial replay): seconds="+elapsed+" recordedGain="+(lastRecordedY-firstY)+" newGain="+(c.State.Position.y-firstY)+" stalledSeconds="+stalled+" endSpeed="+c.State.Speed;
        }
        private static void Set(BirdFlightController c,string field,float value)=>typeof(BirdFlightController).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,value);
    }
}
