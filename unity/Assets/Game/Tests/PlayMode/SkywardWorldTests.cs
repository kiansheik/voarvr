using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoarVR.World;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
namespace VoarVR.Tests
{
    public class SkywardWorldTests
    {
        [Test] public void LandmarkProfilesPreserveOpeningsAndTaperInsteadOfFullBounds()
        {
            foreach(var name in new[]{"Rock","Spire","Log"})
            {
                var profile=SkywardKit.CollisionProfile(name);
                Assert.That(profile.Count,Is.EqualTo(1));
                Assert.That(profile[0].UsesAuthoredMesh,Is.True,name+" should use its exact shared low-poly mesh");
            }
            Assert.That(SkywardKit.CollisionProfile("Tower").Count,Is.EqualTo(8));
            Assert.That(SkywardKit.CollisionProfile("House").Count,Is.EqualTo(4));
            Assert.That(SkywardKit.CollisionProfile("Ruin").Count,Is.EqualTo(5));
            var firstTowerRoof=SkywardKit.CollisionProfile("Tower")[1];
            var firstRuinRoof=SkywardKit.CollisionProfile("Ruin")[1];
            Assert.That(firstTowerRoof.UsesMesh,Is.True);Assert.That(firstTowerRoof.UsesAuthoredMesh,Is.False);
            var sharedRoof=SkywardKit.CollisionMesh(firstTowerRoof,null);
            Assert.That(sharedRoof.vertexCount,Is.EqualTo(8));
            Assert.That(SkywardKit.CollisionMesh(firstRuinRoof,null),Is.SameAs(sharedRoof),
                "Equal authored levels share one cached convex collision hull");
            var leftPillar=SkywardKit.CollisionProfile("Ruin")[2];
            var rearPillar=SkywardKit.CollisionProfile("Ruin")[4];
            Assert.That(leftPillar.RadialSides,Is.EqualTo(6));
            Assert.That(SkywardKit.CollisionMesh(rearPillar,null),Is.SameAs(SkywardKit.CollisionMesh(leftPillar,null)),
                "The two 11m ruin pillars share one cached tapered hex hull");
            foreach(var name in new[]{"Tower","House","Ruin"})
            {
                foreach(var box in SkywardKit.CollisionProfile(name))
                    Assert.That(box.Size.x*box.Size.y*box.Size.z,Is.GreaterThan(0));
                Assert.That(Contains(name,new Vector3(3.9f,3.5f,0)),Is.True,name+" keeps its visible body solid");
            }
            Assert.That(Contains("Ruin",new Vector3(2.15f,5.5f,0)),Is.True,"Ruin follows the visible roof pitch");
            Assert.That(Contains("Ruin",new Vector3(3.53f,4.357f,2.88f)),Is.True,"Ruin's exact lower roof corner remains solid");
            Assert.That(Contains("Ruin",new Vector3(3.75f,4.357f,3.1f)),Is.False,"Air immediately beyond the lower roof corner remains clear");
            Assert.That(Contains("Ruin",new Vector3(3f,5.6f,0)),Is.False,"Ruin does not create a flat full-width roof top");
            Assert.That(Contains("Ruin",new Vector3(0,5.69f,0)),Is.True,"Ruin keeps the authored roof cap solid");
            Assert.That(Contains("Ruin",new Vector3(-8,7,0)),Is.True,"The 11m left pillar side remains solid");
            Assert.That(Contains("Ruin",new Vector3(-6.6f,7,1.4f)),Is.False,"Air beside the tapered hex pillar remains clear");
            Assert.That(Contains("Ruin",new Vector3(-8,10.99f,0)),Is.True,"The real hex pillar cap remains solid");
            Assert.That(Contains("Ruin",new Vector3(8,8,0)),Is.False,"The mirrored 7m pillar has no invisible extension");
            Assert.That(Contains("House",new Vector3(1.9f,10.5f,0)),Is.True,"House follows its final visible roof pitch");
            Assert.That(Contains("House",new Vector3(2.7f,10.6f,0)),Is.False,"House does not create a flat upper roof top");
            Assert.That(Contains("House",new Vector3(0,10.69f,0)),Is.True,"House keeps the authored roof cap solid");
            Assert.That(Contains("Tower",new Vector3(1.45f,20.5f,0)),Is.True,"Tower follows its final visible roof pitch");
            Assert.That(Contains("Tower",new Vector3(2.2f,20.6f,0)),Is.False,"Tower does not create a flat upper roof top");
            Assert.That(Contains("Tower",new Vector3(0,20.69f,0)),Is.True,"Tower keeps the authored roof cap solid");
            Assert.That(Contains("Ruin",Vector3.zero+Vector3.up*2),Is.True);
            Assert.That(Contains("Ruin",new Vector3(5.25f,2,0)),Is.False,"The visible gap beside the ruin must stay traversable");
            Assert.That(Contains("Tower",new Vector3(3.2f,18,0)),Is.False,"Upper tower walk-off follows its narrower silhouette");
            foreach(var name in new[]{"Tree0","Tree1","Tree2"})
                Assert.That(SkywardKit.CollisionProfile(name).Count,Is.EqualTo(6),name+" keeps its trunk and all five branches solid");
            Assert.That(SkywardKit.CollisionProfile("DeadTree").Count,Is.EqualTo(5),
                "DeadTree keeps its trunk and all four branches solid");
            for(int branch=0;branch<5;branch++)
            {
                float angle=branch*2.4f;
                var start=new Vector3(.4f,4.5f+branch*.8f,0);
                var end=new Vector3(Mathf.Cos(angle)*3.8f,start.y+2f,Mathf.Sin(angle)*3f);
                Assert.That(Contains("Tree0",Vector3.Lerp(start,end,.6f)),Is.True,"Tree0 branch "+branch);
            }
            Assert.That(Contains("Tree0",new Vector3(4.5f,12,4.5f)),Is.False,"Leaf volume stays permeable");
            Assert.That(SkywardKit.SurfaceKind("Tree2"),Is.EqualTo(FlightSurfaceKind.Wood));
            Assert.That(SkywardKit.SurfaceKind("Rock"),Is.EqualTo(FlightSurfaceKind.Stone));
            Assert.That(SkywardKit.SurfaceKind("House"),Is.EqualTo(FlightSurfaceKind.Structure));
        }
        private static bool Contains(string name,Vector3 point)
        {
            foreach(var box in SkywardKit.CollisionProfile(name))
            {
                var delta=Quaternion.Inverse(box.Rotation)*(point-box.Center);var half=box.Size*.5f;
                if(box.UsesAuthoredMesh||Mathf.Abs(delta.y)>half.y)continue;
                if(box.SharedMeshKey>0)
                {
                    float height=Mathf.InverseLerp(-half.y,half.y,delta.y);
                    float x=Mathf.Lerp(half.x,box.TopSize.x*.5f,height);
                    float z=Mathf.Lerp(half.z,box.TopSize.y*.5f,height);
                    if(box.RadialSides>0)
                    {
                        float apothem=x*Mathf.Cos(Mathf.PI/box.RadialSides);bool inside=true;
                        for(int edge=0;edge<box.RadialSides;edge++)
                        {
                            float angle=(edge+.5f)*Mathf.PI*2f/box.RadialSides;
                            if(delta.x*Mathf.Cos(angle)+delta.z*Mathf.Sin(angle)>apothem+.0001f){inside=false;break;}
                        }
                        if(inside)return true;
                    }
                    else if(Mathf.Abs(delta.x)<=x+.0001f&&Mathf.Abs(delta.z)<=z+.0001f)return true;
                }
                else if(Mathf.Abs(delta.x)<=half.x&&Mathf.Abs(delta.z)<=half.z)return true;
            }
            return false;
        }

        [UnityTest] public IEnumerator RockAndSpireUseExactSharedMeshesWithOnlyRealLandingFaces()
        {
            var kit=Resources.Load<SkywardKit>("SkywardKit");Assert.That(kit,Is.Not.Null);
            var root=new GameObject("Exact landmark collision fixture");
            try
            {
                var roofGo=new GameObject("Ruin roof");roofGo.layer=WorldStreamer.CollisionLayer;roofGo.transform.SetParent(root.transform,false);
                var roofShape=SkywardKit.CollisionProfile("Ruin")[1];
                var roof=roofGo.AddComponent<MeshCollider>();roof.convex=true;roof.sharedMesh=SkywardKit.CollisionMesh(roofShape,null);
                roofGo.AddComponent<LandingSurface>().SurfaceKind=FlightSurfaceKind.Structure;
                var pillarGo=new GameObject("Ruin pillar");pillarGo.layer=WorldStreamer.CollisionLayer;pillarGo.transform.SetParent(root.transform,false);
                var pillarShape=SkywardKit.CollisionProfile("Ruin")[2];pillarGo.transform.localPosition=pillarShape.Center;
                var pillar=pillarGo.AddComponent<MeshCollider>();pillar.convex=true;pillar.sharedMesh=SkywardKit.CollisionMesh(pillarShape,null);
                pillarGo.AddComponent<LandingSurface>().SurfaceKind=FlightSurfaceKind.Structure;
                foreach(var name in new[]{"Rock","Spire","Log"})
                {
                    var go=new GameObject(name);go.layer=WorldStreamer.CollisionLayer;go.transform.SetParent(root.transform,false);
                    var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=kit.Find(name);
                    var surface=go.AddComponent<LandingSurface>();surface.SurfaceKind=name=="Log"?FlightSurfaceKind.Wood:FlightSurfaceKind.Stone;
                    Assert.That(collider.sharedMesh,Is.SameAs(kit.Find(name)),name+" reuses the authored mesh");
                }
                yield return null;Physics.SyncTransforms();
                Assert.That(roof.Raycast(new Ray(new Vector3(10,.65f,0),Vector3.left),out var roofHit,20),Is.True,
                    "A ray through the visible tapered roof must hit its exact hull");
                Assert.That(roofHit.point.x,Is.EqualTo(2.21f).Within(.08f));
                var roofAir=new Vector3(3,.75f,0);
                Assert.That(Vector3.Distance(roof.ClosestPoint(roofAir),roofAir),Is.GreaterThan(.5f),
                    "The former flat full-width roof top remains air");
                Assert.That(roof.Raycast(new Ray(new Vector3(0,3,0),Vector3.down),out var roofCap,4),Is.True);
                Assert.That(roofCap.normal.y,Is.GreaterThan(.99f),"The real roof cap remains walkable");
                Assert.That(pillar.Raycast(new Ray(new Vector3(-8,15,0),Vector3.down),out var pillarCap,16),Is.True);
                Assert.That(pillarCap.point.y,Is.EqualTo(11f).Within(.01f));Assert.That(pillarCap.normal.y,Is.GreaterThan(.99f));
                Assert.That(pillar.Raycast(new Ray(new Vector3(-12,7,0),Vector3.right),out var pillarSide,8),Is.True);
                Assert.That(pillarSide.normal.y,Is.LessThan(.2f));
                Assert.That(pillar.Raycast(new Ray(new Vector3(-6.6f,15,1.4f),Vector3.down),out _,16),Is.False,
                    "The former rectangular pillar corner remains air");
                var rock=root.transform.Find("Rock").GetComponent<MeshCollider>();
                Assert.That(rock.Raycast(new Ray(new Vector3(10,3.25f,0),Vector3.left),out var rockSide,20),Is.True);
                Assert.That(rockSide.point.x,Is.LessThan(2f),"The former Rock tier top is empty air");
                var spire=root.transform.Find("Spire").GetComponent<MeshCollider>();
                Assert.That(spire.Raycast(new Ray(new Vector3(12,8,0),Vector3.left),out var sideHit,24),Is.True);
                Assert.That(sideHit.point.x,Is.LessThan(4.5f),"The former Spire tier top is empty air");
                Assert.That(sideHit.normal.y,Is.LessThan(.82f),"The actual Spire side is not an invisible walkable step");
                Assert.That(rock.Raycast(new Ray(new Vector3(-1.5f,10,0),Vector3.down),out var topHit,12),Is.True);
                Assert.That(topHit.normal.y,Is.GreaterThan(.99f),"The authored Rock cap remains walkable");
                Assert.That(rock.GetComponent<LandingSurface>().SurfaceKind,Is.EqualTo(FlightSurfaceKind.Stone));
                var log=root.transform.Find("Log").GetComponent<MeshCollider>();
                Assert.That(log.Raycast(new Ray(new Vector3(3,4,-2.5f),Vector3.down),out _,8),Is.False,
                    "Air inside the former Log bounds box stays clear");
                Assert.That(log.Raycast(new Ray(new Vector3(0,4,-.5f),Vector3.down),out var logHit,8),Is.True,
                    "The authored wood branch remains solid");
                Assert.That(logHit.normal.y,Is.GreaterThan(.8f));
                Assert.That(log.GetComponent<LandingSurface>().SurfaceKind,Is.EqualTo(FlightSurfaceKind.Wood));
            }
            finally{Object.Destroy(root);}
        }

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
            {if(!box.enabled)continue;Assert.That(box.center.x,Is.InRange(-20f,20f));Assert.That(box.center.z,Is.InRange(-20f,20f));var before=box.bounds;chunk.SetCollision(false);chunk.SetCollision(true);Physics.SyncTransforms();Assert.That(box.bounds,Is.EqualTo(before));Assert.That(box.transform.parent,Is.EqualTo(root.transform));rotated|=Quaternion.Angle(box.transform.localRotation,Quaternion.identity)>10;Assert.That(box.GetComponent<LandingSurface>().SurfaceKind,Is.Not.EqualTo(FlightSurfaceKind.Terrain));Assert.That(box.Raycast(new Ray(box.transform.TransformPoint(box.center+Vector3.up*(box.size.y+2)),box.transform.TransformDirection(Vector3.down)),out _,box.size.y*2+4),Is.True);}
            Assert.That(rotated,Is.True);Assert.That(chunk.ColliderProxyCount,Is.LessThanOrEqualTo(WorldChunk.MaxColliderProxyCount),
                "Complete tree wood profiles must remain within the bounded streamed-collider budget");
            chunk.DisturbFoliage(root.transform.position,Vector3.right,1);Assert.That(chunk.FoliageImpactCount,Is.EqualTo(1));
            Assert.That(chunk.FoliageImpactStrength,Is.InRange(.34f,.36f));
            Object.Destroy(root);Object.Destroy(mat);yield return null;
        }

        [UnityTest] public IEnumerator CourseRouteOmitsTheKnownSeededSpireButFreeFlightKeepsIt()
        {
            var previousActivity=ActivitySelection.Chosen;
            string previousCourse=ActivitySelection.ChosenCourseId;
            GameObject freeRoot=null,courseRoot=null;
            Material material=null;
            try
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                ActivitySelection.Chosen=FlightActivity.FreeFlight;
                freeRoot=GenerateChunk("Free-flight route fixture",material);
                var seededSpire=new Vector2(121.872f,94.247f);
                Assert.That(NamedColliderCountNear(freeRoot,"Spire collision",seededSpire),Is.GreaterThan(0),
                    "Seed 7319 should retain its known spire outside course mode.");
                var freeChunk=freeRoot.GetComponent<WorldChunk>();freeChunk.SetCollision(true);
                MeshCollider roof=null;
                foreach(var collider in freeRoot.GetComponentsInChildren<MeshCollider>(true))
                    if(collider.sharedMesh!=null&&collider.sharedMesh.name.StartsWith("Shared tapered roof collision",System.StringComparison.Ordinal))
                    {roof=collider;break;}
                Assert.That(roof,Is.Not.Null);Assert.That(roof.enabled,Is.True);
                Assert.That(roof.GetComponent<LandingSurface>().SurfaceKind,Is.EqualTo(FlightSurfaceKind.Structure));
                var sharedRoof=roof.sharedMesh;freeChunk.SetCollision(false);Assert.That(roof.enabled,Is.False);
                freeChunk.SetCollision(true);Assert.That(roof.enabled,Is.True);Assert.That(roof.sharedMesh,Is.SameAs(sharedRoof));

                ActivitySelection.Chosen=FlightActivity.ObstacleCourse;
                ActivitySelection.ChosenCourseId="thermal-ladder";
                courseRoot=GenerateChunk("Reserved course route fixture",material);
                Assert.That(NamedColliderCountNear(courseRoot,"Spire collision",seededSpire),Is.Zero,
                    "The landmark crossing the authored route must be omitted for ranked course play.");
            }
            finally
            {
                ActivitySelection.Chosen=previousActivity;
                ActivitySelection.ChosenCourseId=previousCourse;
                if(freeRoot!=null)Object.Destroy(freeRoot);
                if(courseRoot!=null)Object.Destroy(courseRoot);
                if(material!=null)Object.Destroy(material);
            }
            yield return null;
        }

        private static GameObject GenerateChunk(string name,Material material)
        {
            var root=new GameObject(name);
            var chunk=root.AddComponent<WorldChunk>();
            chunk.Initialize(new[]{material,material,material,material});
            // The reported seed-7319 spire lies at logical (-6.128, 94.247): chunk (-1, 0).
            chunk.Begin(7319,-1,0,Vector3.zero);
            while(!chunk.GenerateStep()){}
            return root;
        }

        private static int NamedColliderCountNear(GameObject root,string prefix,Vector2 localPosition)
        {
            int count=0;
            foreach(var collider in root.GetComponentsInChildren<MeshCollider>(true))
            {
                var p=collider.transform.localPosition;
                if(collider.name.StartsWith(prefix,System.StringComparison.Ordinal)
                    &&Vector2.Distance(new Vector2(p.x,p.z),localPosition)<1f)count++;
            }
            return count;
        }
    }
}
