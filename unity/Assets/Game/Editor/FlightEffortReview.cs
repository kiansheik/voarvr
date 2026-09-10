using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Editor
{
    // Explicit, scene-independent editor measurement. Does not mutate profiles/assets or
    // live flight. Candidate sensitivity scales fixture wrist input only, not rig visuals.
    public static class FlightEffortReview
    {
        private enum WristMotion { Neutral, Persistent20, Cyclic20 }
        private sealed class HumanArc : IFlightInput
        {
            private readonly TrackedBodyFrame body=new TrackedBodyFrame();
            private readonly float amplitude,frequency,twistSensitivity;
            private readonly WristMotion wrist;
            private float time;
            public string Mode => "Calibrated human arc matrix";
            public HumanArc(float a,float f,WristMotion w,float sensitivity)
            { amplitude=a;frequency=f;wrist=w;twistSensitivity=sensitivity; }
            public FlightInputFrame Sample(float dt)
            {
                time+=dt;
                float phase=time*Mathf.PI*2*frequency;
                float h=amplitude*Mathf.Sin(phase);
                float reach=Mathf.Sqrt(.65f*.65f-h*h);
                // Calibration always captures mathematically neutral wrists. Persistent
                // pitch is deliberately applied afterwards; cyclic pitch follows elevation.
                float twist=dt<=0 ? 0 : wrist==WristMotion.Persistent20 ? 20 : wrist==WristMotion.Cyclic20 ? 20*Mathf.Sin(phase) : 0;
                var f=FlightInputFrame.Neutral;
                f.HeadTracked=true;f.HeadPosition=Vector3.up*1.6f;
                f.LeftWing.Position=new Vector3(-reach,1.35f+h,0);
                f.RightWing.Position=new Vector3(reach,1.35f+h,0);
                f.LeftWing.Orientation=f.RightWing.Orientation=Quaternion.Euler(twist*twistSensitivity,0,0);
                body.Sample(ref f,dt);
                return f;
            }
        }
        public static string Run() => Run(1f);
        public static string Run(float dragonTwistSensitivity)
        {
            if(dragonTwistSensitivity<0 || dragonTwistSensitivity>1) throw new ArgumentOutOfRangeException(nameof(dragonTwistSensitivity));
            var csv=new StringBuilder("species,amplitude_m,frequency_hz,wrist_motion,twist_input_scale,min_height_m,end_height_m,end_speed_mps,head_signal,max_landing_brake,mean_stroke_up_N,mean_stroke_up_accel_mps2,end_distance_m\n");
            foreach(string species in new[]{"Duck","Dragon"})
            foreach(float amplitude in new[]{.2f,.3f,.4f})
            foreach(float frequency in new[]{.8f,1.1f,1.5f})
            foreach(WristMotion wrist in Enum.GetValues(typeof(WristMotion)))
            {
                var definition=Resources.Load<BirdCharacterDefinition>("Characters/"+species);
                if(definition==null) throw new InvalidOperationException("Missing species "+species);
                var profile=definition.BuildProfile();
                float sensitivity=species=="Dragon" ? dragonTwistSensitivity : 1;
                var input=new HumanArc(amplitude,frequency,wrist,sensitivity);
                var flight=new BirdFlightController(input,Vector3.up*100,profile:profile);
                flight.Calibrate(input.Sample(0));
                float minHeight=100,brake=0;double strokeUp=0;
                for(int i=0;i<1200;i++)
                {
                    flight.Step(1f/120);
                    minHeight=Mathf.Min(minHeight,flight.State.Position.y);
                    brake=Mathf.Max(brake,flight.LandingBrake);
                    strokeUp+=flight.StrokeForce.y;
                }
                csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F2},{2:F2},{3},{4:F2},{5:F4},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4}\n",
                    species,amplitude,frequency,wrist,sensitivity,minHeight,flight.State.Position.y,flight.State.Speed,
                    flight.HeadPitchInput,brake,strokeUp/1200,strokeUp/1200/profile.MassKg,
                    new Vector2(flight.State.Position.x,flight.State.Position.z).magnitude);
            }
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/reviews/infinite-world-landing-v1"));
            Directory.CreateDirectory(folder);
            string suffix=dragonTwistSensitivity==1 ? "" : "-twist-"+dragonTwistSensitivity.ToString("F2",CultureInfo.InvariantCulture);
            File.WriteAllText(Path.Combine(folder,"effort-matrix"+suffix+".csv"),csv.ToString());
            return csv.ToString();
        }
    }
}
