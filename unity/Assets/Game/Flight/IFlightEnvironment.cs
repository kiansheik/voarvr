using UnityEngine;

namespace VoarVR.Flight
{
    public enum FlightSurfaceKind { Unknown, Terrain, Wood, Foliage, Stone, Structure }

    public struct FlightContact
    {
        public Vector3 Position, Point, Normal;
        public float Distance;
        public bool Landable;
        public int SurfaceId;
        public FlightSurfaceKind SurfaceKind;
        public Collider Collider;
    }

    public struct LandingCandidate
    {
        public Vector3 Position, Normal;
        public int SurfaceId;
        public float Distance;
    }

    // Local render coordinates. Implementations own their bounded spatial queries.
    public interface IFlightEnvironment
    {
        bool Sweep(Vector3 from, Vector3 to, float radius, out FlightContact hit);
        bool TryFindLanding(Vector3 position, float range, out LandingCandidate candidate);
        bool IsSupported(Vector3 position, float radius, int surfaceId);
    }
}
