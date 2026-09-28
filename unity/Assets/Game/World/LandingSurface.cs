using UnityEngine;

namespace VoarVR.World
{
    public sealed class LandingSurface : MonoBehaviour
    {
        public int SurfaceId;
        public VoarVR.Flight.FlightSurfaceKind SurfaceKind = VoarVR.Flight.FlightSurfaceKind.Terrain;
        [Range(0f, 35f)] public float MaxSlopeDegrees = 35f;
    }
}
