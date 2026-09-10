using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class WorldStreamingTests
    {
        [UnityTest]
        public IEnumerator TravelReusesBoundedChunksAndRebasePreservesLogicalIdentity()
        {
            var root = new GameObject("Streaming test");
            var space = root.AddComponent<WorldSpace>();
            var stream = root.AddComponent<WorldStreamer>(); stream.enabled = false;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            stream.Configure(space, material, material, material, material);
            foreach (var location in new[] { Vector3.zero, new Vector3(640, 30, 0), new Vector3(-640, 30, 0), new Vector3(0, 30, 640), new Vector3(0, 30, -640), new Vector3(640, 30, 640), new Vector3(-640, 30, -640), Vector3.zero })
            {
                for (int i = 0; i < 100; i++) stream.TickStreaming(location);
                Assert.That(stream.ActiveCount + stream.PooledCount, Is.EqualTo(WorldStreamer.MaximumChunks));
                Assert.That(stream.IsReadyAt(location), Is.True);
                yield return null;
                Physics.SyncTransforms();
                foreach (var collider in root.GetComponentsInChildren<MeshCollider>())
                {
                    if (!collider.enabled) continue;
                    Assert.That(collider.sharedMesh, Is.Not.Null, "Streamed ground must retain its native collision mesh across frames");
                    Assert.That(collider.bounds.size.y, Is.GreaterThan(0));
                    var origin = collider.transform.position + new Vector3(2, 100, 2);
                    Assert.That(collider.Raycast(new Ray(origin, Vector3.down), out _, 200), Is.True);
                }
            }
            Vector3 before = new Vector3(129, 12, -129); var key = stream.KeyAt(before);
            var logical = space.ToLogical(before); var delta = new Vector3(1024, 0, -1024);
            space.Shift(delta);
            Assert.That(stream.KeyAt(before - delta), Is.EqualTo(key));
            Assert.That(space.ToLogical(before - delta).X, Is.EqualTo(logical.X));
            Assert.That(space.ToLogical(before - delta).Z, Is.EqualTo(logical.Z));
            stream.ResetOrigin();
            Assert.That(space.OffsetX, Is.Zero); Assert.That(space.OffsetZ, Is.Zero);
            Assert.That(stream.IsReadyAt(Vector3.zero), Is.True);
            Assert.That(stream.ActiveCount + stream.PooledCount, Is.EqualTo(WorldStreamer.MaximumChunks));
            Object.Destroy(root); Object.Destroy(material); yield return null;
        }
        [UnityTest]
        public IEnumerator ReloadedChunkHasIdenticalTerrainAndSolidGeometry()
        {
            var go = new GameObject("Chunk deterministic test"); var chunk = go.AddComponent<WorldChunk>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            chunk.Initialize(new[] { material, material, material, material });
            chunk.Begin(7319, -3, 4, Vector3.zero); while (!chunk.GenerateStep()) { }
            var filters = go.GetComponentsInChildren<MeshFilter>();
            var first = new Vector3[filters.Length][];
            for (int i = 0; i < filters.Length; i++) first[i] = filters[i].sharedMesh.vertices;
            // Each canopy solid has six independently shaded faces, all facing outward.
            var canopy = filters[3].sharedMesh;
            var points = canopy.vertices; var normals = canopy.normals;
            for (int box = 0; box < points.Length; box += 24)
            {
                var center = Vector3.zero;
                for (int p = 0; p < 24; p++) center += points[box + p] / 24;
                for (int p = 0; p < 24; p++)
                    Assert.That(Vector3.Dot(points[box + p] - center, normals[box + p]), Is.GreaterThan(0), "Solid face must point outward");
            }
            var firstBoxes = go.GetComponents<BoxCollider>();
            var centers = new Vector3[firstBoxes.Length];
            var sizes = new Vector3[firstBoxes.Length];
            for (int i = 0; i < firstBoxes.Length; i++) { centers[i] = firstBoxes[i].center; sizes[i] = firstBoxes[i].size; }
            chunk.Begin(7319, 8, -9, Vector3.zero); while (!chunk.GenerateStep()) { }
            chunk.Begin(7319, -3, 4, Vector3.zero); while (!chunk.GenerateStep()) { }
            for (int i = 0; i < filters.Length; i++)
                CollectionAssert.AreEqual(first[i], filters[i].sharedMesh.vertices, "Material mesh " + i);
            var reloadedBoxes = go.GetComponents<BoxCollider>();
            for (int i = 0; i < centers.Length; i++)
            {
                Assert.That(reloadedBoxes[i].center, Is.EqualTo(centers[i]));
                Assert.That(reloadedBoxes[i].size, Is.EqualTo(sizes[i]));
            }
            chunk.SetCollision(true);
            Assert.That(go.GetComponentInChildren<MeshCollider>().enabled, Is.True);
            Assert.That(go.GetComponents<BoxCollider>().Length, Is.GreaterThan(0));
            chunk.SetCollision(false);
            foreach (var collider in go.GetComponentsInChildren<Collider>()) Assert.That(collider.enabled, Is.False);
            Object.Destroy(go); Object.Destroy(material); yield return null;
        }
    }
}
