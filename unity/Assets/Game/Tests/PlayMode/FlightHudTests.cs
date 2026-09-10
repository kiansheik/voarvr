using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VoarVR.UI;
using VoarVR.World;
using VoarVR.Flight;

namespace VoarVR.Tests
{
    public sealed class FlightHudTests
    {
        [UnityTest] public IEnumerator InstrumentsAndTrailsSurviveResetAndWorldRebase()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var d=Object.FindFirstObjectByType<BirdFlightDriver>();d.enabled=false;
            var h=d.GetComponent<FlightHud>();var t=d.GetComponent<BirdAirflowTrails>();
            Assert.That(h.Visible,Is.False,"HUD starts hidden");
            h.Toggle();Assert.That(h.Visible,Is.True);
            h.Toggle();Assert.That(h.Visible,Is.False);
            h.SetVisible(true);
            Assert.That(h.Instruments.renderMode,Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(h.Instruments.transform.parent,Is.EqualTo(Camera.main.transform));
            Assert.That(h.Instruments.transform.localPosition.z,Is.EqualTo(2));
            foreach(var text in h.Instruments.GetComponentsInChildren<Text>())
            {Assert.That(text.raycastTarget,Is.False);Assert.That(text.font,Is.Not.Null);}
            d.Controller.SetSpawn(Vector3.up*100);d.Controller.Reset();d.transform.position=d.Controller.State.Position;
            for(int i=0;i<60;i++){d.Tick(1f/60);t.TickTrails();h.TickInstruments();}
            Assert.That(t.VisibleTrails,Is.EqualTo(2));
            var space=Object.FindFirstObjectByType<WorldSpace>();var delta=new Vector3(128,0,128);
            var lines=new[]{t.GetTrail(0),t.GetTrail(1)};var old=lines[0].GetPosition(10);
            d.RebaseWorld(delta);t.TickTrails();
            Assert.That(Vector3.Distance(lines[0].GetPosition(10),old-delta),Is.LessThan(.001));
            d.Controller.Reset();d.transform.position=d.Controller.State.Position;t.TickTrails();h.TickInstruments();
            foreach(var line in lines)
                for(int i=1;i<line.positionCount;i++)Assert.That(Vector3.Distance(line.GetPosition(i),line.GetPosition(0)),Is.LessThan(.001),"Reset must not leave a long trail across the world");
            Assert.That(h.LiftReadout,Is.Not.Empty);
            yield return null;
        }
    }
}
