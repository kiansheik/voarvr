using System;
using System.IO;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Editor
{
    // Explicit editor evidence operations only; never executed on import or in a player.
    public static class WorldReview
    {
        public static string Folder = "../artifacts/reviews/infinite-world-landing-v1/round-01";
        public static string GlideMeasurements()
        {
            var output = new StringBuilder();
            foreach (var species in new[] { "Duck", "Dragon" })
            {
                var profile = Resources.Load<BirdCharacterDefinition>("Characters/" + species).BuildProfile();
                var c = new BirdFlightController(new SyntheticFlightInput(), Vector3.up * 100, profile: profile);
                float time = 0, energy = c.MechanicalEnergy, maximumEnergyGain = 0;
                while (c.State.Position.y > 90 && time < 120)
                {
                    c.Step(1f / 120); time += 1f / 120;
                    maximumEnergyGain = Mathf.Max(maximumEnergyGain, c.MechanicalEnergy - energy); energy = c.MechanicalEnergy;
                }
                float loss = 100 - c.State.Position.y;
                output.AppendLine($"{species}: distance={c.State.Position.z:F5}m, loss={loss:F5}m, ratio={c.State.Position.z/loss:F5}, sink={loss/time:F5}m/s, endAirspeed={c.State.Speed:F5}m/s, elapsed={time:F5}s, maxEnergyGain={maximumEnergyGain:F6}J");
            }
            Directory.CreateDirectory(Folder); File.WriteAllText(Path.Combine(Folder, "glide.txt"), output.ToString());
            return output.ToString();
        }
        public static string ViewAt(Vector3 position, string name, int frames = 300)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<BirdFlightDriver>();
            var s = UnityEngine.Object.FindFirstObjectByType<WorldStreamer>();
            var v = UnityEngine.Object.FindFirstObjectByType<WindVisualizer>();
            d.enabled = false; d.Controller.SetSpawn(position); d.Controller.Reset();
            d.SetSyntheticGesture(SyntheticGesture.Glide, true); d.transform.position = position;
            for (int i = 0; i < 120; i++) s.TickStreaming(position);
            for (int i = 0; i < frames; i++) { s.TickStreaming(d.transform.position); d.Tick(1f/60); v.TickVisuals(); }
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");
            DuckReview.CaptureCurrent(Folder, name);
            return $"{name}: position={d.transform.position}, time={d.Controller.SimulationTime}, ribbons={v.ActiveRibbonCount}, wind={d.Controller.WindVelocity}";
        }
        public static string ContactEvidence()
        {
            var d=UnityEngine.Object.FindFirstObjectByType<BirdFlightDriver>();d.enabled=false;
            var s=UnityEngine.Object.FindFirstObjectByType<WorldStreamer>();
            var wind=UnityEngine.Object.FindFirstObjectByType<WindField>();wind.SetMode(WindMode.StillAir);
            s.ResetOrigin();for(int i=0;i<150;i++)s.TickStreaming(Vector3.zero);
            var output=new StringBuilder();
            BoxCollider roof=null,wall=null;
            foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude))
            {
                if(!box.enabled||box.gameObject.layer!=WorldStreamer.CollisionLayer)continue;
                if(roof==null && Mathf.Abs(box.size.y-1.5f)<.01f && box.bounds.center.magnitude<100)roof=box;
                if(wall==null && box.size.y>15 && box.bounds.center.magnitude<100)wall=box;
            }
            if(roof==null||wall==null)throw new InvalidOperationException("Missing generated review surfaces");
            Vector3 target=roof.bounds.center;target.y=roof.bounds.max.y;
            d.Controller.SetSpawn(target+Vector3.up*.3f);d.Controller.Reset();d.SetSyntheticGesture(SyntheticGesture.Flare,true);
            d.transform.position=d.Controller.State.Position;
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");DuckReview.CaptureCurrent(Folder,"landing-approach");
            for(int i=0;i<240 && d.Controller.State.Phase!=FlightPhase.Perched;i++)d.Tick(1f/120);
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");DuckReview.CaptureCurrent(Folder,"landed");
            output.AppendLine($"Generated canopy top={target}; landed phase={d.Controller.State.Phase}, position={d.transform.position}, speed={d.Controller.State.Speed}, landings={d.Controller.LandingCount}");
            d.SetSyntheticGesture(SyntheticGesture.Flap,true);
            for(int i=0;i<90;i++)d.Tick(1f/120);
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");DuckReview.CaptureCurrent(Folder,"takeoff");
            output.AppendLine($"After flap .75s: phase={d.Controller.State.Phase}, position={d.transform.position}, velocity={d.Controller.State.Velocity}");
            target=wall.bounds.center;target.z=wall.bounds.min.z-3;
            d.Controller.SetSpawn(target);d.Controller.Reset();d.SetSyntheticGesture(SyntheticGesture.Glide,true);d.transform.position=target;
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");DuckReview.CaptureCurrent(Folder,"collision-approach");
            int contacts=d.Controller.CollisionCount;
            for(int i=0;i<240 && d.Controller.CollisionCount==contacts;i++)d.Tick(1f/120);
            Camera.main.GetComponent<VoarVR.Core.FlightCamera>().SendMessage("LateUpdate");DuckReview.CaptureCurrent(Folder,"collision-contact");
            output.AppendLine($"Generated wall frontZ={wall.bounds.min.z:F3}; stopped birdZ={d.transform.position.z:F3}, contacts={d.Controller.CollisionCount-contacts}, speed={d.Controller.State.Speed:F3}, phase={d.Controller.State.Phase}");
            File.WriteAllText(Path.Combine(Folder,"contacts.txt"),output.ToString());wind.SetMode(WindMode.Assisted);
            return output.ToString();
        }
        public static string Traverse()
        {
            var d = UnityEngine.Object.FindFirstObjectByType<BirdFlightDriver>();
            var s = UnityEngine.Object.FindFirstObjectByType<WorldStreamer>();
            var v = UnityEngine.Object.FindFirstObjectByType<WindVisualizer>();
            var wind = UnityEngine.Object.FindFirstObjectByType<WindField>();
            var output = new StringBuilder(); d.enabled=false; wind.SetMode(WindMode.StillAir);
            s.ResetOrigin(); d.Controller.SetSpawn(Vector3.up*500); d.Controller.Reset(); d.SetSyntheticGesture(SyntheticGesture.Glide,true);
            d.transform.position=d.Controller.State.Position;
            for(int i=0;i<120;i++) s.TickStreaming(d.transform.position);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            var timer=System.Diagnostics.Stopwatch.StartNew(); float maxMs=0; int crossings=0, rebases=0;
            var previous=s.KeyAt(d.transform.position); double oldOffset=s.Space.OffsetZ;
            for(int i=0;i<12000;i++)
            {
                s.TickStreaming(d.transform.position); d.Tick(1f/60); v.TickVisuals();
                maxMs=Mathf.Max(maxMs,s.LastGenerationMilliseconds);
                var key=s.KeyAt(d.transform.position); if(!key.Equals(previous)){crossings++;previous=key;}
                if(s.Space.OffsetZ!=oldOffset){rebases++;oldOffset=s.Space.OffsetZ;}
            }
            timer.Stop(); long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
            var p=s.Space.ToLogical(d.transform.position);
            output.AppendLine($"Actual controller still-air flight200s: logical=({p.X:F2},{p.Y:F2},{p.Z:F2}), crossings={crossings}, rebases={rebases}, chunks={s.ActiveCount}, pool={s.PooledCount}, vertices={s.VertexCount}, colliders={s.ColliderProxyCount}, enabledColliders={s.EnabledColliderCount}, generationPeak={maxMs:F3}ms, managedBytes={allocated}, CPUtotal={timer.Elapsed.TotalMilliseconds:F2}ms");
            // Isolate steady non-boundary streaming/visual work after warmup.
            for(int i=0;i<120;i++)s.TickStreaming(d.transform.position);
            bytes=GC.GetAllocatedBytesForCurrentThread(); timer.Restart();
            for(int i=0;i<1000;i++){s.TickStreaming(d.transform.position);v.TickVisuals();}
            timer.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
            output.AppendLine($"1000 steady streamer+visual ticks: managedBytes={allocated}, CPUtotal={timer.Elapsed.TotalMilliseconds:F2}ms");
            output.AppendLine($"Scene objects={UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Length}, renderers={UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).Length}, physicsSaturation={d.GetComponent<UnityFlightEnvironment>().SaturatedQueries}");
            File.WriteAllText(Path.Combine(Folder,"travel.txt"),output.ToString());wind.SetMode(WindMode.Assisted);
            return output.ToString();
        }
    }
}
