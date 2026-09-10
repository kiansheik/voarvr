using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class GeneratedTerrainContactTests
    {
        private GameObject root;
        private UnityFlightEnvironment environment;
        private WorldChunk chunk;
        private Material material;
        private sealed class Input : IFlightInput
        {
            public string Mode => "Terrain fixture";
            public FlightInputFrame Sample(float dt) => FlightInputFrame.Neutral;
        }
        private void Generate(int coordinate)
        {
            root = new GameObject("Generated terrain contact test");
            environment = root.AddComponent<UnityFlightEnvironment>();
            var go = new GameObject("Actual generated chunk"); go.transform.SetParent(root.transform);
            chunk = go.AddComponent<WorldChunk>();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            chunk.Initialize(new[] { material, material, material, material });
            chunk.Begin(42, coordinate, coordinate, new Vector3(coordinate * 128f, 0, coordinate * 128f));
            while (!chunk.Ready) chunk.GenerateStep();
            chunk.SetCollision(true);
            // This fixture isolates generated terrain; authored scenery contact has separate coverage.
            foreach(var box in chunk.GetComponentsInChildren<BoxCollider>())box.enabled=false;
            environment.ConfigureTerrain(_ => chunk);
            Assert.That(chunk.GetComponentInChildren<MeshCollider>().sharedMesh, Is.Not.Null, "Ready terrain must bind its completed collision mesh");
            Physics.SyncTransforms();
        }
        private Vector3 GroundPoint()
        {
            var mesh = chunk.GetComponentInChildren<MeshCollider>();
            var origin = chunk.transform.position + new Vector3(2, 100, 2);
            Assert.That(mesh.Raycast(new Ray(origin, Vector3.down), out var hit, 200), Is.True, "Generated mesh must face upward and answer a top ray");
            return hit.point;
        }
        [TearDown] public void Cleanup()
        {
            if(root != null)
            {
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) if(filter.sharedMesh != null) Object.DestroyImmediate(filter.sharedMesh);
                Object.DestroyImmediate(root);
            }
            if(material != null) Object.DestroyImmediate(material);
        }
        [TestCase(0)] [TestCase(2)] [TestCase(-2)]
        public void ActualGeneratedTerrainStopsFastAboveToBelowSweep(int coordinate)
        {
            Generate(coordinate); var p = GroundPoint();
            Assert.That(environment.Sweep(p + Vector3.up * 20, p - Vector3.up * 20, .55f, out var contact), Is.True);
            Assert.That(contact.Normal.y, Is.GreaterThan(.8f));
            Assert.That(contact.Position.y, Is.GreaterThanOrEqualTo(p.y + .55f));
        }
        [TestCase(.1f)] [TestCase(-.1f)] [TestCase(-4f)]
        public void ActualGeneratedTerrainRecoversOverlapAndBelowSurface(float offset)
        {
            Generate(-2); var p = GroundPoint();
            Assert.That(environment.Sweep(p + Vector3.up * offset, p + Vector3.up * (offset - 1), .55f, out var contact), Is.True);
            Assert.That(contact.Normal.y, Is.GreaterThan(.8f));
            Assert.That(contact.Position.y, Is.GreaterThanOrEqualTo(p.y + .55f));
        }
        [TestCase("Duck")] [TestCase("Dragon")]
        public void RebasedGeneratedTerrainStillStopsDescendingController(string species)
        {
            Generate(2);
            chunk.transform.position -= new Vector3(256, 0, 256); Physics.SyncTransforms();
            var p = GroundPoint();
            var definition=Resources.Load<BirdCharacterDefinition>("Characters/"+species);
            Assert.That(definition,Is.Not.Null);
            var profile = definition.BuildProfile(); profile.InitialSpeedMps = 0;
            var controller = new BirdFlightController(new Input(), p + Vector3.up * 2, profile:profile, environment:environment);
            for(int i=0;i<600;i++)
            {
                controller.Step(1f/120f);
                var position=controller.State.Position;
                Assert.That(chunk.TryGetGround(position,out var surfacePoint,out var normal,out _),Is.True);
                float separation=Vector3.Dot(position-surfacePoint,normal);
                Assert.That(separation, Is.GreaterThanOrEqualTo(profile.CollisionRadius - .001f),
                    "Sphere must remain above the current terrain triangle while moving across the slope");
            }
            Assert.That(controller.CollisionCount, Is.GreaterThan(0), "Hard descent must produce a collision rather than pass through terrain");
        }
        [TestCase("Duck")] [TestCase("Dragon")]
        public void GentleContactOnGeneratedTerrainPerches(string species)
        {
            Generate(-2); var p=GroundPoint();
            var definition=Resources.Load<BirdCharacterDefinition>("Characters/"+species);
            Assert.That(definition,Is.Not.Null);
            var profile=definition.BuildProfile(); profile.InitialSpeedMps=0;
            Assert.That(chunk.TryGetGround(p,out _,out var normal,out _),Is.True);
            var spawn=p+Vector3.up*(profile.CollisionRadius/normal.y+.1f);
            var controller=new BirdFlightController(new Input(),spawn,profile:profile,environment:environment);
            for(int i=0;i<240 && controller.State.Phase!=FlightPhase.Perched;i++)controller.Step(1f/120f);
            Assert.That(controller.State.Phase,Is.EqualTo(FlightPhase.Perched),"A gentle physical approach must settle onto generated ground");
            Assert.That(controller.State.Speed,Is.EqualTo(0));
            Assert.That(chunk.TryGetGround(controller.State.Position,out var point,out normal,out _),Is.True);
            Assert.That(Vector3.Dot(controller.State.Position-point,normal),Is.GreaterThanOrEqualTo(profile.CollisionRadius-.001f));
        }
        [Test]
        public void RecycledChunkAndReenabledCollisionKeepNativeMeshBound()
        {
            Generate(0);
            var collider=chunk.GetComponentInChildren<MeshCollider>();
            GroundPoint();
            chunk.Begin(42,-2,-2,new Vector3(-256,0,-256));
            Assert.That(collider.sharedMesh,Is.Null);
            while(!chunk.Ready)chunk.GenerateStep();
            chunk.SetCollision(true); Physics.SyncTransforms();
            Assert.That(collider.sharedMesh,Is.Not.Null);
            GroundPoint();
            chunk.SetCollision(false); collider.sharedMesh=null;
            chunk.SetCollision(true); Physics.SyncTransforms();
            Assert.That(collider.sharedMesh,Is.Not.Null);
            GroundPoint();
        }
    }
}
