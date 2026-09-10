using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoarVR.World;
using VoarVR.Flight;
using VoarVR.Input;
namespace VoarVR.Tests
{
    public class SkywardWorldTests
    {
        [UnityTest] public IEnumerator IslandIdentityAndSolidGardenSurvivePoolTravelAndRebase()
        {
            var root=new GameObject("Sky fixture");var space=root.AddComponent<WorldSpace>();space.Shift(new Vector3(80000,0,80000));
            var sky=root.AddComponent<SkyArchipelago>();sky.enabled=false;sky.Configure(space);
            Vector3 p=space.ToLocal(420,271,620);sky.Tick(p);yield return null;Physics.SyncTransforms();
            var surfaces=root.GetComponentsInChildren<LandingSurface>();LandingSurface garden=null;
            foreach(var s in surfaces)if(Vector3.Distance(s.transform.position,space.ToLocal(420,270,620))<1)garden=s;
            Assert.That(garden,Is.Not.Null);int id=garden.SurfaceId;var mesh=garden.GetComponent<MeshCollider>().sharedMesh;
            sky.Tick(space.ToLocal(769,271,620));Assert.That(garden.SurfaceId,Is.EqualTo(id));Assert.That(garden.GetComponent<MeshCollider>().sharedMesh,Is.SameAs(mesh));
            var delta=new Vector3(1024,0,-1024);space.Shift(delta);p-=delta;sky.Tick(p);Physics.SyncTransforms();Assert.That(garden.SurfaceId,Is.EqualTo(id));
            var env=root.AddComponent<UnityFlightEnvironment>();Assert.That(env.Sweep(p+Vector3.up*10,p-Vector3.up*10,.22f,out var hit),Is.True);Assert.That(hit.Normal.y,Is.GreaterThan(.8f));
            var profile=BirdFlightProfile.Duck();profile.InitialSpeedMps=0;var c=new BirdFlightController(new SyntheticFlightInput(),p,profile:profile,environment:env);
            for(int i=0;i<240 && c.State.Phase!=FlightPhase.Perched;i++)c.Step(1f/120);
            Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
            // The hero tower is in the same collision mesh as its visible geometry.
            var tower=space.ToLocal(443,280,636);Assert.That(env.Sweep(tower+Vector3.forward*12,tower-Vector3.forward*12,.22f,out _),Is.True);
            Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator AuthoredCollisionUsesPlacementRotation()
        {
            var root=new GameObject("Authored collision fixture");var chunk=root.AddComponent<WorldChunk>();var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));chunk.Initialize(new[]{mat,mat,mat,mat});chunk.Begin(7319,4,4,new Vector3(90000,0,90000));while(!chunk.GenerateStep()){}chunk.SetCollision(true);yield return null;Physics.SyncTransforms();
            bool rotated=false;foreach(var box in root.GetComponentsInChildren<BoxCollider>())
            {if(!box.enabled)continue;Assert.That(box.center.x,Is.InRange(-20f,20f));Assert.That(box.center.z,Is.InRange(-20f,20f));var before=box.bounds;chunk.SetCollision(false);chunk.SetCollision(true);Physics.SyncTransforms();Assert.That(box.bounds,Is.EqualTo(before));Assert.That(box.transform.parent,Is.EqualTo(root.transform));rotated|=Quaternion.Angle(box.transform.localRotation,Quaternion.identity)>10;Assert.That(box.Raycast(new Ray(box.transform.TransformPoint(box.center+Vector3.up*(box.size.y+2)),box.transform.TransformDirection(Vector3.down)),out _,box.size.y*2+4),Is.True);}
            Assert.That(rotated,Is.True);Object.Destroy(root);Object.Destroy(mat);yield return null;
        }
    }
}
