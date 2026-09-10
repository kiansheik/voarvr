using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class WindVisibilityTests
    {
        [UnityTest]
        public IEnumerator AssistedWindIsVisibleAheadDuringActualFlightAndStillAirHidesPool()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var stream=Object.FindAnyObjectByType<WorldStreamer>();stream.enabled=false;
            var visual=Object.FindAnyObjectByType<WindVisualizer>();visual.enabled=false;
            var wind=Object.FindAnyObjectByType<WindField>();wind.SetMode(WindMode.Assisted);
            var spawn=new Vector3(28,56,50);
            driver.Controller.SetSpawn(spawn);driver.Controller.Reset();
            driver.transform.position=spawn;driver.SetViewMode(FlightViewMode.ThirdPerson);
            for(int i=0;i<150;i++) stream.TickStreaming(spawn);
            // Advance the real simulation and actual trace histories, not manually posed lines.
            for(int i=0;i<180;i++)
            {
                driver.Tick(1f/60);stream.TickStreaming(driver.transform.position);visual.TickVisuals();
            }
            yield return null; // Let the actual chase camera follow the final flight state.
            int visibleAhead=0;
            var lines=visual.GetComponentsInChildren<LineRenderer>().Where(line=>line.name.StartsWith("Air ribbon ")).ToArray();
            Assert.That(lines.Length,Is.EqualTo(WindVisualizer.RibbonCapacity));
            foreach(var line in lines)
            {
                if(!line.enabled || line.startColor.a<.15f) continue;
                var p=Camera.main.WorldToViewportPoint(line.GetPosition(0));
                if(p.z>0 && p.x>.02f && p.x<.98f && p.y>.02f && p.y<.98f) visibleAhead++;
            }
            Assert.That(visibleAhead,Is.GreaterThanOrEqualTo(2),"Assisted airflow needs readable traces in the forward view, not only offscreen features.");
            Assert.That(visual.SamplesLastFrame,Is.LessThanOrEqualTo(48));
            wind.SetMode(WindMode.StillAir);visual.TickVisuals();
            foreach(var line in lines) Assert.That(line.enabled,Is.False);
        }
    }
}
