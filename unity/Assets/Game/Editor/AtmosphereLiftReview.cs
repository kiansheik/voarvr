using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Editor
{
    public static class AtmosphereLiftReview
    {
        private sealed class Circle:IFlightInput
        {
            public float Bank,Delay; private float elapsed;
            public string Mode=>"Lift envelope review";
            public FlightInputFrame Sample(float dt) { var f=FlightInputFrame.Neutral;elapsed+=dt;f.Bank=elapsed>=Delay ? Bank : 0;return f; }
        }
        private sealed class SmoothVerticalLimit:IWindField
        {
            public float Cap,HorizontalScale=1;
            public string ModeName=>"Candidate smooth vertical envelope";
            public Vector3 Sample(Vector3 p,float time)
            {
                var w=AtmosphereModel.SampleLogical(p.x,p.y,p.z,time,7319,WindMode.Assisted);
                w.x*=HorizontalScale;w.z*=HorizontalScale;
                if(Cap>0) w.y=Cap*(float)Math.Tanh(w.y/Cap);
                return w;
            }
        }
        public static string RunEntry()
        {
            var report=new StringBuilder("species,cap,horizontalScale,bank,deltaHeight,endVertical,peakAlpha\n");
            foreach(string species in new[]{"Duck","Dragon"})
            foreach(float cap in new[]{0f,3f,5f})
            foreach(float horizontal in new[]{1f,.3f})
            foreach(float bank in new[]{0f,.25f,.55f})
            {
                var plume=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);var core=plume.Center(0,plume.Altitude);
                var spawn=new Vector3((float)core.X,(float)core.Y+10,(float)core.Z-80);
                var c=new BirdFlightController(new Circle { Bank=bank,Delay=10 },spawn,
                    profile:Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile(),
                    wind:new SmoothVerticalLimit { Cap=cap,HorizontalScale=horizontal });
                float alpha=0;
                for(int i=0;i<4800;i++){c.Step(1f/120);alpha=Mathf.Max(alpha,Mathf.Abs(c.AngleOfAttackDeg));}
                report.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F3},{5:F3},{6:F3}\n",species,cap,horizontal,bank,c.State.Position.y-spawn.y,c.State.Velocity.y,alpha);
            }
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/reviews/flight-instruments-v1/round-01/lift-entry-matrix.csv"));
            File.WriteAllText(path,report.ToString());return report.ToString();
        }
        public static string RunFeathering()
        {
            var report=new StringBuilder("species,start,trim,bank,heightDelta,endVertical,minAirspeed,maxAlpha\n");
            var diagnostics=new StringBuilder("time,height,groundSpeed,airSpeed,alpha,windX,windY,windZ\n");
            foreach(string species in new[]{"Duck","Dragon"})
            foreach(bool entry in new[]{false,true})
            foreach(int trim in new[]{0,1,2})
            foreach(float bank in new[]{0f,.25f,.55f})
            {
                var plume=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);var core=plume.Center(0,plume.Altitude);
                var spawn=new Vector3((float)core.X-(entry ? 0 : 8),(float)core.Y+(entry ? 10 : 0),(float)core.Z-(entry ? 80 : 0));
                var profile=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
                var wind=new SmoothVerticalLimit();
                var c=new BirdFlightController(new Circle { Bank=bank,Delay=entry ? 10 : 0 },spawn,profile:profile,wind:wind);
                float alpha=0,minAirspeed=100;
                int steps=entry ? 4800 : 2400;
                for(int i=0;i<steps;i++)
                {
                    var state=c.State;var air=wind.Sample(state.Position,c.SimulationTime);
                    var forward=state.Rotation*Vector3.forward;var up=state.Rotation*Vector3.up;
                    var relative=state.Velocity-air;
                    float pathAir=Mathf.Atan2(Vector3.Dot(relative,up),Vector3.Dot(relative,forward))*Mathf.Rad2Deg;
                    float pathGround=Mathf.Atan2(Vector3.Dot(state.Velocity,up),Vector3.Dot(state.Velocity,forward))*Mathf.Rad2Deg;
                    profile.WingIncidenceDeg=8;
                    if(trim==1) profile.WingIncidenceDeg+=Mathf.Clamp(pathAir-pathGround,-35,35);
                    if(trim==2) profile.WingIncidenceDeg-=Mathf.Clamp(8-pathAir-20,0,45);
                    c.Step(1f/120);alpha=Mathf.Max(alpha,Mathf.Abs(c.AngleOfAttackDeg));minAirspeed=Mathf.Min(minAirspeed,relative.magnitude);
                    if(species=="Duck" && entry && trim==0 && bank==0 && i<600 && i%30==0)
                        diagnostics.AppendFormat(CultureInfo.InvariantCulture,"{0:F2},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3}\n",i/120f,state.Position.y,state.Speed,relative.magnitude,c.AngleOfAttackDeg,air.x,air.y,air.z);
                }
                report.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3:F2},{4:F3},{5:F3},{6:F3},{7:F3}\n",species,entry ? "entry40s" : "core20s",trim,bank,c.State.Position.y-spawn.y,c.State.Velocity.y,minAirspeed,alpha);
            }
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/reviews/flight-instruments-v1/round-01"));
            File.WriteAllText(Path.Combine(folder,"feathering-matrix.csv"),report.ToString());
            File.WriteAllText(Path.Combine(folder,"duck-entry-diagnostics.csv"),diagnostics.ToString());
            return report.ToString();
        }
        public static string Run() => Run(false);
        public static string Run(bool gentle)
        {
            var report=new StringBuilder("species,cap_mps,bank,offset_m,height_change_m,still_height_change_m,min_height_change_m,end_vertical_mps,peak_abs_alpha_deg\n");
            foreach(string species in new[]{"Duck","Dragon"})
            foreach(float cap in gentle ? new[]{1f,1.5f,2f,2.5f} : new[]{0f,3f,3.5f,4f,4.5f,5f})
            foreach(float bank in new[]{0f,.25f,.55f})
            foreach(float offset in new[]{8f,22f})
            {
                var plume=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);
                var core=plume.Center(0,plume.Altitude);
                var spawn=new Vector3((float)core.X-offset,(float)core.Y,(float)core.Z);
                var profile=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
                var input=new Circle { Bank=bank };
                var c=new BirdFlightController(input,spawn,profile:profile,wind:new SmoothVerticalLimit { Cap=cap });
                var calm=new BirdFlightController(input,spawn,profile:profile);
                float min=spawn.y,alpha=0;
                for(int i=0;i<2400;i++)
                {
                    c.Step(1f/120);calm.Step(1f/120);
                    min=Mathf.Min(min,c.State.Position.y);alpha=Mathf.Max(alpha,Mathf.Abs(c.AngleOfAttackDeg));
                }
                report.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F1},{2:F2},{3:F0},{4:F3},{5:F3},{6:F3},{7:F3},{8:F2}\n",
                    species,cap,bank,offset,c.State.Position.y-spawn.y,calm.State.Position.y-spawn.y,min-spawn.y,c.State.Velocity.y,alpha);
            }
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/reviews/flight-instruments-v1/round-01/lift-envelope-matrix"+(gentle ? "-gentle" : "")+".csv"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,report.ToString());return report.ToString();
        }
    }
}
