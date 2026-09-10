using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.Tests
{
    public sealed class FlightContactTests
    {
        // Half-space ground plus a thin wall; sweeps deliberately accept arbitrarily long steps.
        private sealed class Room : IFlightEnvironment
        {
            public bool Sweep(Vector3 from, Vector3 to, float radius, out FlightContact hit)
            {
                hit = default;
                float fraction = 2f;
                if (to.y < radius && from.y >= radius)
                {
                    fraction = (from.y - radius) / (from.y - to.y);
                    hit = new FlightContact { Position = Vector3.Lerp(from, to, fraction) + Vector3.up * FlightContactSolver.Skin,
                        Point = Vector3.Lerp(from, to, fraction) - Vector3.up * radius,
                        Normal = Vector3.up, Landable = true, SurfaceId = 4 };
                }
                if (to.x > 5f - radius && from.x <= 5f - radius)
                {
                    float t = (5f - radius - from.x) / (to.x - from.x);
                    if (t < fraction)
                    {
                        fraction = t;
                        hit = new FlightContact { Position = Vector3.Lerp(from, to, t) + Vector3.left * FlightContactSolver.Skin,
                            Normal = Vector3.left, Landable = false, SurfaceId = 8 };
                    }
                }
                hit.Distance = Vector3.Distance(from, to) * fraction;
                return fraction <= 1f;
            }
            public bool TryFindLanding(Vector3 p, float r, out LandingCandidate c) { c = default; return false; }
            public bool IsSupported(Vector3 p, float r, int id) => id == 4 && Mathf.Abs(p.y-r) < .05f;
        }

        [Test]
        public void FastStepCannotTunnelThroughThinWall()
        {
            var r = FlightContactSolver.Resolve(new Room(), new Vector3(0, 10, 0), Vector3.right * 100, 1, .22f, false);
            Assert.That(r.Position.x, Is.LessThan(5f - .22f));
            Assert.That(r.Velocity.x, Is.EqualTo(0f).Within(.001f));
            Assert.That(r.Landed, Is.False);
        }

        [Test]
        public void SafeContactLandsAtActualSurface()
        {
            var r = FlightContactSolver.Resolve(new Room(), new Vector3(0, 1, 0), new Vector3(0, -2, 3), 1, .22f, false);
            Assert.That(r.Landed, Is.True);
            Assert.That(r.Position.y, Is.EqualTo(.235f).Within(.001f));
            Assert.That(r.SurfaceId, Is.EqualTo(4));
            Assert.That(r.Velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ExcessiveImpactSlidesWithoutCapturingOrAddingEnergy()
        {
            var velocity = new Vector3(0, -12, 20);
            var r = FlightContactSolver.Resolve(new Room(), new Vector3(0, 1, 0), velocity, .2f, .55f, true);
            Assert.That(r.Landed, Is.False);
            Assert.That(r.Velocity.y, Is.EqualTo(0f));
            Assert.That(r.Velocity.sqrMagnitude, Is.LessThan(velocity.sqrMagnitude));
            Assert.That(r.Position.y, Is.GreaterThanOrEqualTo(.55f));
        }

        [Test]
        public void BrakingWidensOnlyMeasuredContactLimits()
        {
            var contact = new FlightContact { Landable = true, Normal = Vector3.up };
            Assert.That(FlightContactSolver.CanLand(contact, new Vector3(7, -3, 0), false), Is.False);
            Assert.That(FlightContactSolver.CanLand(contact, new Vector3(7, -3, 0), true), Is.True);
            Assert.That(FlightContactSolver.CanLand(contact, new Vector3(8.1f, -3, 0), true), Is.False);
            contact.Normal = Quaternion.Euler(40, 0, 0) * Vector3.up;
            Assert.That(FlightContactSolver.CanLand(contact, Vector3.down, true), Is.False);
        }

        [Test]
        public void UpwardTakeoffCannotBeCaptured()
        {
            var contact = new FlightContact { Landable = true, Normal = Vector3.up };
            Assert.That(FlightContactSolver.CanLand(contact, Vector3.up * 2.5f, true), Is.False);
            var r = FlightContactSolver.Resolve(new Room(), new Vector3(0, .235f, 0), new Vector3(0, 2.5f, 6), .2f, .22f, false);
            Assert.That(r.Position.y, Is.GreaterThan(.7f));
            Assert.That(r.ContactCount, Is.EqualTo(0));
        }

        [Test]
        public void CornerResolvesBothContactsWithoutCrossingEitherSurface()
        {
            var r = FlightContactSolver.Resolve(new Room(), new Vector3(0, 1, 0), new Vector3(20, -10, 5), 1, .22f, false);
            Assert.That(r.ContactCount, Is.EqualTo(2));
            Assert.That(r.Position.x, Is.LessThan(4.78f));
            Assert.That(r.Position.y, Is.GreaterThan(.22f));
            Assert.That(r.Landed, Is.False);
        }
    }
}
