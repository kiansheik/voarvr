using System.IO;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Editor
{
    // Explicit Play-mode inspection of the selected model on native streamed terrain.
    public static class GroundExperienceReview
    {
        public static string Folder="../artifacts/reviews/comfort-ground-feedback/round-01";
        private sealed class Input : IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Ground evidence";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        public static string Capture()
        {
            var d=Object.FindFirstObjectByType<BirdFlightDriver>();d.enabled=false;
            var rig=d.GetComponent<BirdRigDriver>();
            string species=rig.leftUpper.root.GetComponentsInChildren<Renderer>().Any(r=>r.name=="DragonBody")?"Dragon":"Duck";
            var profile=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
            profile.InitialSpeedMps=0; // Drop onto the review roof; do not fly over its edge.
            var s=Object.FindFirstObjectByType<WorldStreamer>();s.ResetOrigin();
            for(int i=0;i<150;i++)s.TickStreaming(Vector3.zero);
            var roof=Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude).First(b=>b.enabled && b.gameObject.layer==WorldStreamer.CollisionLayer && Mathf.Abs(b.size.y-1.5f)<.01f && b.bounds.center.magnitude<100);
            var start=roof.bounds.center;start.y=roof.bounds.max.y+profile.CollisionRadius+.01f;
            var input=new Input();
            var c=new BirdFlightController(input,start,profile:profile,environment:d.GetComponent<UnityFlightEnvironment>());
            typeof(BirdFlightDriver).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,input);
            typeof(BirdFlightDriver).GetProperty("Controller").SetValue(d,c);
            for(int i=0;i<180;i++)d.Tick(1f/120);
            if(c.State.Phase!=FlightPhase.Perched)throw new System.InvalidOperationException("Ground capture requires actual contact");
            d.StartCoroutine(CaptureFrames(d,input,c,species));
            return species+" capture started; real frames separate each pose.";
        }
        private static IEnumerator CaptureFrames(BirdFlightDriver d,Input input,BirdFlightController c,string species)
        {
            var rig=d.GetComponent<BirdRigDriver>();
            var report=new StringBuilder();
            var ground=d.GetComponent<BirdGroundPresentation>();
            report.AppendLine($"{species}: native roof phase={c.State.Phase}, position={c.State.Position}, legs={ground.LegCount}, blend={ground.GroundBlend}, landingCount={c.LandingCount}, cues={d.GetComponent<FlightFeedback>().ContactCueCount}");
            float size=species=="Dragon"?2.7f:1;
            void Snap(string name)
            {
                d.GetComponent<BirdAirflowTrails>().TickTrails();
                DuckReview.Capture(Folder,species+"-"+name,new Vector3(1.2f,.5f,-1.8f)*size,Vector3.zero);
                DuckReview.Capture(Folder,species+"-"+name+"-side",new Vector3(1.6f,.16f,-.4f)*size,Vector3.down*.10f);
            }
            // Skinning render data is cached per frame. Never capture several changed
            // poses with camera.Render in one frame and mistake stale skinning for motion.
            yield return null;yield return null;Snap("settled");
            input.Frame.GroundMove=Vector2.up;
            for(int frame=0;frame<80;frame++)
            {
                d.Tick(1f/120);
                if(frame==19 || frame==39 || frame==79){yield return null;yield return null;Snap("walk-"+frame);}
            }
            report.AppendLine($"After walk: {c.State.Position}, velocity={c.GroundVelocity}, phase={c.State.Phase}, cues={d.GetComponent<FlightFeedback>().ContactCueCount}");
            input.Frame.GroundMove=Vector2.zero;
            input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*2;
            for(int i=0;i<30;i++)d.Tick(1f/120);
            yield return null;yield return null;Snap("takeoff");
            report.AppendLine($"After takeoff: {c.State.Position}, velocity={c.State.Velocity}, phase={c.State.Phase}, blend={ground.GroundBlend}, wing reach error={rig.LeftReachError}");
            Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,species+"-ground.txt"),report.ToString());
        }
    }
}
