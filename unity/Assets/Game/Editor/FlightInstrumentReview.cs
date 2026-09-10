using System.IO;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;
using VoarVR.UI;

namespace VoarVR.Editor
{
    public static class FlightInstrumentReview
    {
        public static string Folder="../artifacts/reviews/flight-instruments-v1/round-01";
        public static string Capture(Vector3 position,string name,int frames=300, SyntheticGesture gesture=SyntheticGesture.Glide)
        {
            var d=Object.FindFirstObjectByType<BirdFlightDriver>();d.enabled=false;
            var s=Object.FindFirstObjectByType<WorldStreamer>();s.ResetOrigin();
            d.Controller.SetSpawn(position);d.Controller.Reset();d.transform.position=position;
            d.SetSyntheticGesture(gesture,true);
            for(int i=0;i<140;i++)s.TickStreaming(position);
            var wind=Object.FindFirstObjectByType<WindVisualizer>();
            var hud=d.GetComponent<FlightHud>();var trails=d.GetComponent<BirdAirflowTrails>();
            for(int i=0;i<frames;i++)
            {
                s.TickStreaming(d.transform.position);d.Tick(1f/60);wind.TickVisuals();trails.TickTrails();hud.TickInstruments();
            }
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");
            Directory.CreateDirectory(Folder);DuckReview.CaptureCurrent(Folder,name);
            string result=$"{name}: position={d.transform.position}, air={d.Controller.WindVelocity}, vertical={d.Controller.State.Velocity.y:F3}, airspeed={FlightHud.AirspeedKnots(d.Controller.State.Velocity,d.Controller.WindVelocity):F2}kt, trails={trails.VisibleTrails}, ribbons={wind.ActiveRibbonCount}, hud={hud.LiftReadout}";
            File.WriteAllText(Path.Combine(Folder,name+".txt"),result);return result;
        }
        private sealed class Circle:IFlightInput
        {
            public string Mode=>"Unpowered thermal circle";
            public FlightInputFrame Sample(float dt){var f=FlightInputFrame.Neutral;f.Bank=.55f;return f;}
        }
        public static string MeasureLift()
        {
            var go=new GameObject("Instrument lift measurements");var field=go.AddComponent<WindField>();
            var report=new StringBuilder();
            try
            {
                foreach(string species in new[]{"Duck","Dragon"})
                foreach(float offset in new[]{8f,22f})
                {
                    var p=AtmosphereModel.GetPlume(7319,0,0,-1,WindMode.Assisted);var core=p.Center(0,p.Altitude);
                    var spawn=new Vector3((float)core.X-offset,(float)core.Y,(float)core.Z);
                    var profile=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
                    var c=new BirdFlightController(new Circle(),spawn,profile:profile,wind:field);
                    var calm=new BirdFlightController(new Circle(),spawn,profile:profile);
                    for(int i=0;i<2400;i++){c.Step(1f/120);calm.Step(1f/120);}
                    report.AppendLine($"{species}, offset={offset}:20s no flap gain={c.State.Position.y-spawn.y:F3}m, stillAir={calm.State.Position.y-spawn.y:F3}m, finalVertical={c.State.Velocity.y:F3}m/s");
                }
            }
            finally{Object.DestroyImmediate(go);}
            Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"lift-measurements.txt"),report.ToString());return report.ToString();
        }
    }
}
