using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    // One adapter per bird, bounded buffers, no global landing registry or Rigidbody bird.
    public sealed class UnityFlightEnvironment : MonoBehaviour, IFlightEnvironment
    {
        public const int CollisionLayer = 30;
        public const int CollisionMask = 1 << CollisionLayer;
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private readonly Collider[] overlaps = new Collider[64];
        private SphereCollider probe;
        private System.Func<Vector3, WorldChunk> terrainLookup;
        public void ConfigureTerrain(System.Func<Vector3, WorldChunk> lookup) => terrainLookup = lookup;
        private void Start()
        {
            if(terrainLookup != null) return;
            var streamer=FindAnyObjectByType<WorldStreamer>();
            if(streamer != null) terrainLookup = p => streamer.TryGetReadyChunk(p,out var chunk) ? chunk : null;
        }
        public int LastCandidateCount { get; private set; }
        public int SaturatedQueries { get; private set; }

        private void OnDestroy()
        {
            if (probe == null) return;
            if (Application.isPlaying) Destroy(probe.gameObject);
            else DestroyImmediate(probe.gameObject);
        }

        private void Awake()
        {
            // Keep a live native collision shape for ComputePenetration. A disabled
            // collider created in EditMode can have no native shape yet and return
            // false even when the overlap query proves penetration.
            var go = new GameObject("Flight penetration probe") { hideFlags = HideFlags.HideAndDontSave, layer = 2 };
            go.transform.SetParent(transform, false);
            probe = go.AddComponent<SphereCollider>();
            probe.isTrigger = true;
        }

        private LandingSurface Surface(Collider collider) => collider.GetComponentInParent<LandingSurface>();

        private FlightContact Contact(Collider collider, Vector3 center, Vector3 point, Vector3 normal, float distance)
        {
            var surface = Surface(collider);
            return new FlightContact { Position = center, Point = point, Normal = normal,
                Distance = distance, SurfaceId = surface != null ? surface.SurfaceId : 0,
                Landable = surface != null && normal.y >= Mathf.Cos(surface.MaxSlopeDegrees * Mathf.Deg2Rad) };
        }

        public bool Sweep(Vector3 from, Vector3 to, float radius, out FlightContact hit)
        {
            hit = default;
            // Open terrain meshes are one-sided surfaces. Recover below/overlapping
            // starts from the exact generated triangle, rather than asking a triangle
            // soup for an arbitrary penetration side or applying a global floor clamp.
            var ground=terrainLookup?.Invoke(from);
            if(ground != null && ground.TryGetGround(from,out var point,out var normal,out int surfaceId))
            {
                float separation=Vector3.Dot(from-point,normal);
                if(separation < radius)
                {
                    var safe=from+Vector3.up*((radius-separation+FlightContactSolver.Skin)/normal.y);
                    hit=new FlightContact {Position=safe,Point=point,Normal=normal,Distance=0,
                        Landable=normal.y>=Mathf.Cos(FlightContactSolver.MaximumSlopeDegrees*Mathf.Deg2Rad),SurfaceId=surfaceId};
                    return true;
                }
            }
            if (probe == null) Awake();
            probe.radius = radius;
            int count = Physics.OverlapSphereNonAlloc(from, radius, overlaps, CollisionMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return SaturatedContact(from, to, out hit);
            float deepest = 0f;
            for (int i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                Vector3 direction;
                float depth;
                bool penetrates = collider is BoxCollider box
                    ? BoxPenetration(box, from, radius, out direction, out depth)
                    : Physics.ComputePenetration(probe, from, Quaternion.identity, collider, collider.transform.position,
                        collider.transform.rotation, out direction, out depth);
                if (penetrates && depth > deepest)
                {
                    deepest = depth;
                    Vector3 safe = from + direction * (depth + FlightContactSolver.Skin);
                    hit = Contact(collider, safe, safe - direction * radius, direction, 0f);
                }
            }
            if (deepest > 0f) return true;
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < .000001f) return false;
            count = Physics.SphereCastNonAlloc(from, radius, delta / distance, hits, distance, CollisionMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return SaturatedContact(from, to, out hit);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.distance >= closest || Vector3.Dot(delta, h.normal) >= 0f) continue;
                closest = h.distance;
                hit = Contact(h.collider, from + delta.normalized * Mathf.Max(0f, h.distance - FlightContactSolver.Skin),
                    h.point, h.normal, h.distance);
            }
            return closest < float.PositiveInfinity;
        }

        private static bool BoxPenetration(BoxCollider box, Vector3 position, float radius, out Vector3 normal, out float depth)
        {
            // Exact sphere/OBB minimum translation, including centres inside the box.
            // This also avoids depending on native-shape creation timing in EditMode.
            Quaternion rotation = box.transform.rotation;
            Vector3 local = Quaternion.Inverse(rotation) * (position - box.transform.TransformPoint(box.center));
            Vector3 scale = box.transform.lossyScale;
            Vector3 half = Vector3.Scale(box.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Vector3 closest = new Vector3(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y), Mathf.Clamp(local.z, -half.z, half.z));
            Vector3 delta = local - closest;
            float distance = delta.magnitude;
            if (distance > .000001f)
            {
                normal = rotation * (delta / distance); depth = radius - distance;
                return depth > 0f;
            }
            Vector3 clearance = half - new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            int axis = clearance.x < clearance.y ? 0 : 1;
            if (clearance.z < clearance[axis]) axis = 2;
            Vector3 axisNormal = Vector3.zero;
            axisNormal[axis] = local[axis] < 0f ? -1f : 1f;
            normal = rotation * axisNormal;
            depth = radius + clearance[axis];
            return true;
        }

        private bool SaturatedContact(Vector3 from, Vector3 to, out FlightContact hit)
        {
            // NonAlloc result order is unspecified. A full buffer must never let an
            // omitted nearer collider turn into tunnelling: stop conservatively.
            SaturatedQueries++;
            Vector3 normal = (from - to).normalized;
            if (normal.sqrMagnitude < .5f) normal = Vector3.up;
            hit = new FlightContact { Position = from, Point = from, Normal = normal };
            return true;
        }

        public bool TryFindLanding(Vector3 position, float range, out LandingCandidate candidate)
        {
            candidate = default;
            int count = Physics.OverlapSphereNonAlloc(position, range, overlaps, CollisionMask, QueryTriggerInteraction.Ignore);
            LastCandidateCount = count;
            if (count == overlaps.Length) SaturatedQueries++;
            float best = range;
            for (int i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                var surface = Surface(collider);
                if (surface == null) continue;
                // Probe the actual collider top rather than its centre/capture bubble.
                var bounds = collider.bounds;
                Vector3 above = new Vector3(Mathf.Clamp(position.x, bounds.min.x, bounds.max.x), bounds.max.y + .1f,
                    Mathf.Clamp(position.z, bounds.min.z, bounds.max.z));
                if (!collider.Raycast(new Ray(above, Vector3.down), out var h, bounds.size.y + .2f)) continue;
                if (h.normal.y < Mathf.Cos(surface.MaxSlopeDegrees * Mathf.Deg2Rad)) continue;
                float d = Vector3.Distance(position, h.point);
                if (d >= best || h.point.y > position.y + .5f) continue;
                best = d;
                candidate = new LandingCandidate { Position = h.point, Normal = h.normal, Distance = d, SurfaceId = surface.SurfaceId };
            }
            return best < range;
        }

        public bool IsSupported(Vector3 position, float radius, int surfaceId)
        {
            return Sweep(position, position + Vector3.down * (FlightContactSolver.Skin * 3f + .025f), radius, out var hit)
                && hit.Landable && hit.SurfaceId == surfaceId;
        }
    }
}
