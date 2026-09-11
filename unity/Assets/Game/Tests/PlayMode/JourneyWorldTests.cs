using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Tests
{
    public class JourneyWorldTests
    {
        [UnityTest]
        public IEnumerator FarGardenKeepsItsArchAndTowerAcrossTheHysteresisBand()
        {
            var root=new GameObject("Journey landmark fixture");var space=root.AddComponent<WorldSpace>();
            space.Shift(new Vector3(80000,0,80000));var sky=root.AddComponent<SkyArchipelago>();sky.enabled=false;sky.Configure(space);
            var garden=space.ToLocal(420,270,620);sky.Tick(garden);
            Transform island=null;
            foreach(var surface in root.GetComponentsInChildren<LandingSurface>())
                if(Vector3.Distance(surface.transform.position,garden)<1)island=surface.transform;
            Assert.That(island,Is.Not.Null);
            var near=island.Find("Garden and arch").GetComponent<MeshRenderer>();
            var far=island.Find("Distant silhouette").GetComponent<MeshRenderer>();
            var silhouette=far.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(silhouette.bounds.min.z,Is.LessThan(-67),"The real approach arch survives at distance.");
            Assert.That(silhouette.bounds.max.y,Is.GreaterThanOrEqualTo(44),"The 45 m tower survives at distance.");
            sky.Tick(garden+Vector3.forward*530);Assert.That(near.enabled,Is.True);
            sky.Tick(garden+Vector3.forward*570);Assert.That(far.enabled,Is.True);
            sky.Tick(garden+Vector3.forward*530);Assert.That(far.enabled,Is.True,"No LOD flutter inside the band.");
            sky.Tick(garden+Vector3.forward*470);Assert.That(near.enabled,Is.True);
            Object.Destroy(root);yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicalSeedAndRestoredCrownSurviveRebaseAndPresentationReload()
        {
            var root=new GameObject("Journey state fixture");var space=root.AddComponent<WorldSpace>();
            var view=root.AddComponent<JourneyPresentation>();view.Configure(space,null);
            var challenge=new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(Observation(100,130,154));challenge.Step(Observation(100,231,154));
            Assert.That(challenge.Kind,Is.EqualTo(ObjectiveKind.CollectSeed));
            var seed=RouteHomeChapter.SeedPosition;var player=space.ToLocal(seed.X,seed.Y,seed.Z-20);
            view.Present(player,challenge,false,true,10);
            Assert.That(view.SeedVisible,Is.True);Assert.That(view.SeedWorldPosition,Is.EqualTo(space.ToLocal(seed.X,seed.Y,seed.Z)));
            var delta=new Vector3(1024,0,-1024);space.Shift(delta);player-=delta;
            Assert.That(view.SeedWorldPosition,Is.EqualTo(space.ToLocal(seed.X,seed.Y,seed.Z)),"Rebase is immediate, before the next update.");
            challenge.Step(Observation(seed.X,seed.Y,seed.Z));
            view.Present(player,challenge,false,false,11);
            Assert.That(challenge.SeedCollected,Is.True);Assert.That(view.SeedVisible,Is.False);Assert.That(view.GuideVisible,Is.False);
            view.Present(player,challenge,true,false,12);view.Present(player,challenge,true,false,16);
            Assert.That(view.BloomVisible,Is.True);var crown=view.CrownWorldPosition;
            Object.Destroy(view);yield return null;
            view=root.AddComponent<JourneyPresentation>();view.Configure(space,null);view.Present(player,new FlightChallenge(FlightActivity.FreeFlight),true,false,0);
            Assert.That(view.BloomVisible,Is.True,"Saved restoration is visible even outside the chapter.");
            Assert.That(view.CrownWorldPosition,Is.EqualTo(crown));Assert.That(view.GuideVisible,Is.False);
            var meshes=root.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(meshes.Length,Is.EqualTo(5),"One bounded set of chapter visuals after replacement.");
            foreach(var filter in meshes)Assert.That(filter.sharedMesh.vertexCount,Is.LessThanOrEqualTo(480));
            Object.Destroy(root);yield return null;
        }

        [UnityTest]
        public IEnumerator ActualTickKeepsTheLiftRouteVisibleAboveTheColumnAndStillAirDoesNotAdvertiseLift()
        {
            var previousActivity=ActivitySelection.Chosen;bool previousResume=ActivitySelection.ResumeRequested;
            bool previousGuidance=FlightPreferences.GuidanceEnabled;
            ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;
            FlightPreferences.GuidanceEnabled=true;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var world=Object.FindAnyObjectByType<WorldStreamer>();world.enabled=false;
            var wind=Object.FindAnyObjectByType<WindField>();wind.SetMode(WindMode.Assisted);
            var view=driver.GetComponent<JourneyPresentation>();
            try
            {
                driver.Controller.StreamingBlocked=false;driver.Controller.SetPaused(false);
                view.Tick(new Vector3(0,25,0));
                Assert.That(view.NavigationAvailable,Is.True,"Departure has a useful route even before entering the lift.");
                Assert.That(view.TargetMarkerVisible,Is.True);
                Assert.That(view.TargetMarkerKind,Is.EqualTo(ObjectiveKind.DiscoverLift));
                driver.Expedition.Challenge.Step(Observation(100,130,154));
                Assert.That(driver.Expedition.Challenge.Kind,Is.EqualTo(ObjectiveKind.Soar));
                view.Tick(new Vector3(300,480,154));
                Assert.That(view.NavigationAvailable,Is.True,"A high/outside player is directed back into real air instead of losing the route.");
                Assert.That(view.GuideVisible,Is.True);Assert.That(view.TargetMarkerVisible,Is.True);
                Assert.That(view.NavigationTarget.Y,Is.LessThan(480));
                Assert.That(wind.Sample(view.TargetMarkerWorldPosition,driver.Controller.SimulationTime).y,Is.GreaterThan(1.5f));
                var logical=world.Space.ToLogical(view.TargetMarkerWorldPosition);
                Assert.That(logical.X,Is.EqualTo(driver.Expedition.Challenge.Target(driver.Controller.SimulationTime).X).Within(.001));
                wind.SetMode(WindMode.StillAir);view.Tick(new Vector3(300,480,154));
                Assert.That(view.NavigationAvailable,Is.False);Assert.That(view.TargetMarkerVisible,Is.False);Assert.That(view.GuideVisible,Is.False);
                wind.SetMode(WindMode.Assisted);FlightPreferences.GuidanceEnabled=false;view.Tick(new Vector3(300,480,154));
                Assert.That(view.TargetMarkerVisible,Is.False,"The existing guidance preference still controls world cues.");
            }
            finally
            {
                FlightPreferences.GuidanceEnabled=previousGuidance;
                ActivitySelection.Chosen=previousActivity;ActivitySelection.ResumeRequested=previousResume;
            }
            yield return SceneManager.LoadSceneAsync("CharacterSelect");yield return null;
        }

        [UnityTest]
        public IEnumerator StageMarkersKeepTheirRealTargetsAtArrivalAndPickupTransfersOnlyForContinuousPlay()
        {
            var root=new GameObject("Journey guidance fixture");var space=root.AddComponent<WorldSpace>();
            var view=root.AddComponent<JourneyPresentation>();view.Configure(space,null);
            var challenge=new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(Observation(100,130,154));challenge.Step(Observation(100,231,154));
            var pickup=RouteHomeChapter.SeedPosition;var position=space.ToLocal(pickup.X,pickup.Y,pickup.Z);
            view.Present(new Vector3(1047,318,-483),challenge,false,true,9.8f);
            Assert.That(view.SeedVisible,Is.False,"The physical collectible remains bounded at long range.");
            Assert.That(view.NavigationAvailable,Is.True,"The actual Quest seed-stage position must still offer a route.");
            Assert.That(view.GuideVisible,Is.True);Assert.That(view.TargetMarkerVisible,Is.True,"The target pin has no collectible-distance cutoff.");
            Assert.That(view.TargetMarkerWorldPosition,Is.EqualTo(position));
            view.Present(position-Vector3.forward*10,challenge,false,true,10);
            Assert.That(view.TargetMarkerKind,Is.EqualTo(ObjectiveKind.CollectSeed));
            Assert.That(view.TargetMarkerWorldPosition,Is.EqualTo(position));
            Assert.That(view.CarryVisible,Is.False);
            view.Present(position-Vector3.forward*2,challenge,false,true,10.1f);
            Assert.That(view.GuideVisible,Is.False,"The final approach leaves the pickup aperture unobstructed.");
            Assert.That(view.TargetMarkerVisible,Is.True);
            challenge.Step(Observation(pickup.X,pickup.Y,pickup.Z));
            view.Present(position,challenge,false,true,10.2f);
            Assert.That(view.CarryVisible,Is.True);Assert.That(view.SeedVisible,Is.False);
            Assert.That(view.PickupTransferActive,Is.True);Assert.That(view.CarryWorldPosition,Is.EqualTo(position));
            Assert.That(view.TargetMarkerKind,Is.EqualTo(ObjectiveKind.Precision));
            var arch=challenge.Target(10.2);
            Assert.That(view.TargetMarkerWorldPosition,Is.EqualTo(space.ToLocal(arch.X,arch.Y,arch.Z)));
            view.Present(position,challenge,false,true,10.65f);view.Present(position,challenge,false,true,11.15f);
            Assert.That(view.PickupTransferActive,Is.False);
            var carry=root.transform.Find("A Route Home presentation/Carried story seed");var rotation=carry.rotation;
            view.Present(position,challenge,false,true,11.25f);Assert.That(carry.rotation,Is.EqualTo(rotation),"Possession does not spin like a moth.");
            var saved=challenge.Capture();var resumed=new FlightChallenge(FlightActivity.RouteHome);Assert.That(resumed.Restore(saved),Is.True);
            view.Present(position,resumed,false,true,11.35f);
            Assert.That(view.CarryVisible,Is.True);Assert.That(view.PickupTransferActive,Is.False,"Resume is already held, not another pickup.");
            saved.Stage=4;Assert.That(resumed.Restore(saved),Is.True);
            var garden=resumed.Destination;var landing=space.ToLocal(garden.X,garden.Y,garden.Z);
            view.Present(landing,resumed,false,true,11.45f);
            Assert.That(view.TargetMarkerKind,Is.EqualTo(ObjectiveKind.Land));Assert.That(view.TargetMarkerVisible,Is.True);
            Assert.That(view.TargetMarkerWorldPosition,Is.EqualTo(landing),"Landing marker remains on the actual terrace all the way to contact.");
            var delta=new Vector3(1024,0,-1024);space.Shift(delta);
            Assert.That(view.TargetMarkerWorldPosition,Is.EqualTo(landing-delta));
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(5));
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))Assert.That(filter.sharedMesh.vertexCount,Is.LessThanOrEqualTo(480));
            Object.Destroy(root);yield return null;
        }

        [UnityTest]
        public IEnumerator CloudFamiliesStayBoundedAndKeepTheChapterCorridorClear()
        {
            var root=new GameObject("Journey cloud fixture");var space=root.AddComponent<WorldSpace>();
            var weather=root.AddComponent<SkyWeatherPresentation>();weather.enabled=false;weather.Configure(space);weather.Tick(Vector3.zero);
            var families=new HashSet<Mesh>();int banks=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                banks++;families.Add(filter.sharedMesh);Assert.That(filter.sharedMesh.vertexCount,Is.LessThanOrEqualTo(364));
                if(filter.gameObject.activeSelf)
                {var p=space.ToLogical(filter.transform.position);Assert.That(SkyWeatherPresentation.ClearsFirstRoute(p.X,p.Z),Is.True);}
            }
            Assert.That(banks,Is.EqualTo(16));Assert.That(families.Count,Is.EqualTo(3));
            foreach(var family in families)
            {
                // A closed sculpted cloud has two faces at every mesh edge. This
                // catches a longitude crack even when both sides look nearly joined.
                var edges=new Dictionary<ulong,int>();var indices=family.triangles;
                for(int i=0;i<indices.Length;i+=3)for(int j=0;j<3;j++)
                {
                    uint a=(uint)indices[i+j],b=(uint)indices[i+(j+1)%3];
                    ulong edge=((ulong)System.Math.Min(a,b)<<32)|System.Math.Max(a,b);
                    edges.TryGetValue(edge,out int count);edges[edge]=count+1;
                }
                foreach(var count in edges.Values)Assert.That(count,Is.EqualTo(2),family.name+" must be closed at its seam and poles.");
            }
            Assert.That(SkyWeatherPresentation.ClearsFirstRoute(140,200),Is.False);
            Assert.That(SkyWeatherPresentation.ClearsFirstRoute(420,620),Is.False);
            var delta=new Vector3(2048,0,-1024);space.Shift(delta);weather.Tick(-delta);
            Assert.That(root.GetComponentsInChildren<MeshFilter>(true).Length,Is.EqualTo(16));
            Object.Destroy(root);yield return null;
        }

        private static ChallengeObservation Observation(double x,double y,double z)
            =>new ChallengeObservation(new LogicalPosition(x,y,z),Vector3.forward*12,4,1,1,false,0,0);
    }
}
