using UnityEngine;

namespace VoarVR.Flight
{
    // Deterministic contact fixture and optional simple ground; never a capture radius.
    public sealed class PlaneFlightEnvironment : IFlightEnvironment
    {
        public readonly float Height;
        public PlaneFlightEnvironment(float height) { Height = height; }

        public bool Sweep(Vector3 from, Vector3 to, float radius, out FlightContact hit)
        {
            hit = default;
            float centerHeight = Height + radius;
            bool penetrating = from.y < centerHeight;
            if (!penetrating && (to.y >= centerHeight || to.y >= from.y)) return false;
            float fraction = penetrating ? 0f : (from.y - centerHeight) / (from.y - to.y);
            Vector3 center = Vector3.Lerp(from, to, fraction);
            center.y = centerHeight + FlightContactSolver.Skin;
            hit = new FlightContact { Position = center, Point = new Vector3(center.x, Height, center.z),
                Normal = Vector3.up, Distance = Vector3.Distance(from, to) * fraction, Landable = true, SurfaceId = 1 };
            return true;
        }

        public bool TryFindLanding(Vector3 position, float range, out LandingCandidate candidate)
        {
            float distance = Mathf.Abs(position.y - Height);
            candidate = new LandingCandidate { Position = new Vector3(position.x, Height, position.z),
                Normal = Vector3.up, Distance = distance, SurfaceId = 1 };
            return distance < range && position.y >= Height;
        }

        public bool IsSupported(Vector3 position, float radius, int surfaceId) =>
            surfaceId == 1 && Mathf.Abs(position.y - Height - radius) <= FlightContactSolver.Skin * 3f;
    }
}
