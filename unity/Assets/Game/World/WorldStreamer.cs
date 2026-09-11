using System;
using System.Collections.Generic;
using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    public readonly struct ChunkKey : IEquatable<ChunkKey>
    {
        public readonly long X, Z;
        public ChunkKey(long x, long z) { X = x; Z = z; }
        public bool Equals(ChunkKey other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is ChunkKey other && Equals(other);
        public override int GetHashCode() => unchecked((int)(X ^ (X >> 32)) * 397 ^ (int)(Z ^ (Z >> 32)));
    }
    public sealed class WorldStreamer : MonoBehaviour
    {
        public const int CollisionLayer = 30;
        public const int Radius = 2;
        public const int MaximumChunks = 25;
        public WorldSpace Space { get; private set; }
        public int ActiveCount => active.Count;
        public int PooledCount => pool.Count;
        public int PendingCount => queue.Count;
        public int VertexCount { get { int count = 0; foreach (var chunk in active.Values) count += chunk.VertexCount; return count; } }
        public int ColliderProxyCount { get { int count = 0; foreach (var chunk in active.Values) count += chunk.ColliderProxyCount; foreach (var chunk in pool) count += chunk.ColliderProxyCount; return count; } }
        public int EnabledColliderCount { get { int count = 0; foreach (var chunk in active.Values) count += chunk.EnabledColliderCount; return count; } }
        public float LastGenerationMilliseconds { get; private set; }
        public float PeakGenerationMilliseconds { get; private set; }
        public float GenerationBudgetMilliseconds = 1.5f;
        private readonly Dictionary<ChunkKey, WorldChunk> active = new Dictionary<ChunkKey, WorldChunk>();
        private readonly Stack<WorldChunk> pool = new Stack<WorldChunk>();
        private readonly List<ChunkKey> remove = new List<ChunkKey>();
        private readonly Queue<WorldChunk> queue = new Queue<WorldChunk>();
        private Material[] materials;
        private BirdFlightDriver driver;
        private ChunkKey center;
        private bool hasCenter;
        private Vector3? recoveryFocus;
        // A paused journey recovery loads its destination before moving the bird.
        public void SetRecoveryFocus(Vector3? localPosition) => recoveryFocus = localPosition;
        public void Configure(WorldSpace space, Material ground, Material city, Material forest, Material canopy)
        {
            Space = space; materials = new[] { ground, city, forest, canopy };
            Space.Rebased += Rebase;
            driver = FindAnyObjectByType<BirdFlightDriver>();
            TickStreaming(recoveryFocus ?? (driver != null ? driver.transform.position : Vector3.zero));
            // Only central chunk is built synchronously for safe initial contact.
            var first = active[center]; while (!first.Ready) first.GenerateStep(); first.SetCollision(true);
        }
        private void Update()
        {
            if (Space == null) return;
            if (driver == null) driver = FindAnyObjectByType<BirdFlightDriver>();
            TickStreaming(recoveryFocus ?? (driver != null ? driver.transform.position : Vector3.zero));
        }
        public ChunkKey KeyAt(Vector3 local)
        {
            var p = Space.ToLogical(local);
            return new ChunkKey((long)Math.Floor(p.X / WorldTerrain.ChunkSize), (long)Math.Floor(p.Z / WorldTerrain.ChunkSize));
        }
        public bool TryGetReadyChunk(Vector3 position, out WorldChunk chunk)
        {
            chunk = null;
            return Space != null && active.TryGetValue(KeyAt(position), out chunk) && chunk.Ready;
        }
        public bool IsReadyAt(Vector3 position) => Space != null && active.TryGetValue(KeyAt(position), out var chunk) && chunk.Ready;
        public void TickStreaming(Vector3 position)
        {
            if (Space == null) return;
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            double millisecondsPerTick = 1000d / System.Diagnostics.Stopwatch.Frequency;
            var next = KeyAt(position);
            if (!hasCenter || !center.Equals(next))
            {
                center = next; hasCenter = true; remove.Clear(); queue.Clear();
                foreach (var pair in active)
                    if (Math.Abs(pair.Key.X - center.X) > Radius || Math.Abs(pair.Key.Z - center.Z) > Radius) remove.Add(pair.Key);
                foreach (var key in remove) { var chunk = active[key]; chunk.gameObject.SetActive(false); pool.Push(chunk); active.Remove(key); }
                // Concentric priority, center first. Rebuild queue instead of stale jobs on pooled roots.
                for (int ring = 0; ring <= Radius; ring++)
                    for (int z = -ring; z <= ring; z++) for (int x = -ring; x <= ring; x++)
                    {
                        if (Math.Max(Math.Abs(x), Math.Abs(z)) != ring) continue;
                        var key = new ChunkKey(center.X + x, center.Z + z);
                        if (!active.TryGetValue(key, out var chunk))
                        {
                            if (pool.Count > 0) chunk = pool.Pop();
                            else { var go = new GameObject("StreamedChunk"); go.transform.SetParent(transform, false); chunk = go.AddComponent<WorldChunk>(); chunk.Initialize(materials); }
                            chunk.Begin(Space.Seed, key.X, key.Z, Space.ToLocal(key.X * 128d, 0, key.Z * 128d)); active.Add(key, chunk);
                        }
                        chunk.SetCollision(ring <= 1);
                        if (!chunk.Ready) queue.Enqueue(chunk);
                    }
            }
            int work = 0;
            while (queue.Count > 0 && work++ < 32 && (System.Diagnostics.Stopwatch.GetTimestamp() - started) * millisecondsPerTick < GenerationBudgetMilliseconds)
            {
                var chunk = queue.Peek();
                if (chunk.GenerateStep()) queue.Dequeue();
            }
            if (work > 0) Physics.SyncTransforms();
            LastGenerationMilliseconds = (float)((System.Diagnostics.Stopwatch.GetTimestamp() - started) * millisecondsPerTick);
            PeakGenerationMilliseconds = Mathf.Max(PeakGenerationMilliseconds, LastGenerationMilliseconds);
        }
        public void ResetOrigin()
        {
            recoveryFocus = null;
            queue.Clear();
            foreach (var chunk in active.Values) { chunk.gameObject.SetActive(false); pool.Push(chunk); }
            active.Clear(); hasCenter = false; Space.ResetOrigin();
            TickStreaming(Vector3.zero);
            var first = active[center]; while (!first.Ready) first.GenerateStep(); first.SetCollision(true);
            Physics.SyncTransforms();
        }
        public void Rebase(Vector3 delta)
        {
            if (recoveryFocus.HasValue) recoveryFocus -= delta;
            foreach (var chunk in active.Values) chunk.transform.position -= delta;
            foreach (var chunk in pool) chunk.transform.position -= delta;
            Physics.SyncTransforms();
        }
        private void OnDestroy() { if (Space != null) Space.Rebased -= Rebase; }
    }
}
