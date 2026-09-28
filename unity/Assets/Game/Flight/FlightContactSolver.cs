using UnityEngine;

namespace VoarVR.Flight
{
    public struct FlightContactResult
    {
        public Vector3 Position, Velocity, Point, Normal;
        public bool Landed, UnsafeLanding;
        // Side is body-relative: -1 left, +1 right, 0 centred/top/bottom.
        public float ImpactSpeed, Side;
        public int SurfaceId, ContactCount, UnsafeSurfaceId;
        public FlightSurfaceKind SurfaceKind;
        public Collider Collider;
    }

    public static class FlightContactSolver
    {
        public const float MaximumSlopeDegrees = 35f;
        public const float Skin = .015f;
        public const int MaximumContacts = 4;
        public static float SafeDownwardSpeed(bool braking) => braking ? 3.2f : 2.5f;
        public static float SafeHorizontalSpeed(bool braking) => braking ? 8f : 6f;
        public static bool CanLand(FlightContact contact, Vector3 velocity, bool braking)
        {
            if (!contact.Landable || Vector3.Dot(contact.Normal, Vector3.up) < Mathf.Cos(MaximumSlopeDegrees * Mathf.Deg2Rad)) return false;
            // A surface cannot capture a bird departing from it, even inside contact skin.
            if (Vector3.Dot(velocity, contact.Normal) > .05f) return false;
            // Top-surface contact is an automatic landing, not a scored manoeuvre.
            // Walls, steep faces and upward takeoff still cannot capture the bird.
            return true;
        }

        public static FlightContactResult Resolve(IFlightEnvironment environment, Vector3 from, Vector3 velocity,
            float dt, float radius, bool braking, bool allowLanding = true, Vector3 bodyRight = default)
        {
            var result = new FlightContactResult { Position = from, Velocity = velocity };
            if (bodyRight.sqrMagnitude < .5f) bodyRight = Vector3.right;
            else bodyRight.Normalize();
            float remaining = Mathf.Max(0f, dt);
            for (int i = 0; i < MaximumContacts; i++)
            {
                Vector3 displacement = result.Velocity * remaining;
                if (!environment.Sweep(result.Position, result.Position + displacement, radius, out var contact))
                { result.Position += displacement; break; }
                result.Position = contact.Position;
                result.ContactCount++;
                float impact = Mathf.Max(0f, -Vector3.Dot(result.Velocity, contact.Normal));
                // Feedback follows the strongest surface in a multi-contact solve, not
                // whichever glancing contact happened to consume the final iteration.
                if (result.ContactCount == 1 || impact >= result.ImpactSpeed)
                {
                    result.ImpactSpeed = impact;
                    result.Point = contact.Point;
                    result.Normal = contact.Normal;
                    result.Side = Mathf.Clamp(Vector3.Dot(-contact.Normal, bodyRight), -1f, 1f);
                    result.SurfaceId = contact.SurfaceId;
                    result.SurfaceKind = contact.SurfaceKind;
                    result.Collider = contact.Collider;
                }
                if (allowLanding && CanLand(contact, result.Velocity, braking))
                { result.Landed = true; result.Velocity = Vector3.zero; break; }
                if (allowLanding && contact.Landable && !CanLand(contact, result.Velocity, braking))
                { result.UnsafeLanding = true; result.UnsafeSurfaceId = contact.SurfaceId; }
                allowLanding = false;
                float travelledFraction = displacement.magnitude > .00001f ? Mathf.Clamp01(contact.Distance / displacement.magnitude) : 0f;
                remaining *= 1f - travelledFraction;
                float incoming = Vector3.Dot(result.Velocity, contact.Normal);
                if (incoming < 0f) result.Velocity -= contact.Normal * incoming;
                // Dissipative glancing contact. No hidden launch impulse or speed gain.
                result.Velocity *= .88f;
                if (remaining <= .00001f || result.Velocity.sqrMagnitude < .000001f) break;
            }
            // If the contact budget is exhausted, keep the last safe position.
            return result;
        }
    }
}
