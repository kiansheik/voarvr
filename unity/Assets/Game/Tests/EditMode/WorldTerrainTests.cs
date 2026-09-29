using NUnit.Framework;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class WorldTerrainTests
    {
        [Test]
        public void GlobalFieldsAreDeterministicAcrossNegativeChunkEdgesAndLargeCoordinates()
        {
            foreach (double x in new[] { -128d, 0d, 128d, 1000000128d, -1000000128d })
            {
                float left = WorldTerrain.Elevation(7319, x - .001, 29);
                float right = WorldTerrain.Elevation(7319, x + .001, 29);
                Assert.That(right, Is.EqualTo(left).Within(.01));
                Assert.That(WorldTerrain.Hash(7319, (long)x, -19), Is.EqualTo(WorldTerrain.Hash(7319, (long)x, -19)));
            }
            Assert.That(WorldTerrain.Hash(12, -1, 1), Is.Not.EqualTo(WorldTerrain.Hash(12, 1, -1)));
            Assert.That(WorldTerrain.Hash(12, -1, 1), Is.Not.EqualTo(WorldTerrain.Hash(13, -1, 1)));
        }
        [Test]
        public void RiverAndBiomeCrossChunkBoundariesContinuously()
        {
            for (int z = -1024; z <= 1024; z += 128)
            {
                Assert.That(WorldTerrain.RiverCenter(7319, z + .001), Is.EqualTo(WorldTerrain.RiverCenter(7319, z - .001)).Within(.01));
                Assert.That(WorldTerrain.Biome(7319, 128 + .001, z), Is.EqualTo(WorldTerrain.Biome(7319, 128 - .001, z)).Within(.001));
            }
            Assert.That(WorldTerrain.Elevation(7319, 0, 0), Is.LessThan(0));
        }
        [Test]
        public void DepartureLookoutIsFlatClearAndOverlooksTheFirstLift()
        {
            const int seed = 7319;
            double x = FlightRegions.DepartureLookoutX, z = FlightRegions.DepartureLookoutZ;
            Assert.That(System.Math.Sqrt(x * x + z * z), Is.GreaterThan(85), "Outside the enclosed spawn bowl");
            Assert.That(WorldTerrain.RiverDistance(seed, x, z), Is.GreaterThan(12));
            float perch = WorldTerrain.Elevation(seed, x, z);
            for (int dx = -3; dx <= 3; dx += 3)
                for (int dz = -3; dz <= 3; dz += 3)
                    Assert.That(WorldTerrain.Elevation(seed, x + dx, z + dz), Is.EqualTo(perch).Within(.75f), "Flat supported perch");
            double thermalX = 0, thermalZ = 0;
            for (int quarter = 0; quarter < 4; quarter++)
            {
                var thermal = FlightRegions.DepartureThermal(quarter * System.Math.PI / 2 / .007);
                thermalX += thermal.X / 4; thermalZ += thermal.Z / 4;
            }
            float bearing = (float)(System.Math.Atan2(thermalX - x, thermalZ - z) * 180 / System.Math.PI);
            Assert.That(UnityEngine.Mathf.DeltaAngle(bearing, FlightRegions.DepartureLookoutHeading), Is.InRange(-5f, 5f),
                "The first view faces the departure lift");
            // The chase eye sits about 1.2m above the perch. Across the central view the
            // terrain stays below eye level, unlike every direction from the spawn floor.
            float eye = perch + 1.2f;
            for (int angle = -25; angle <= 25; angle += 5)
                for (int distance = 5; distance <= 100; distance += 5)
                {
                    double yaw = (FlightRegions.DepartureLookoutHeading + angle) * System.Math.PI / 180;
                    float ground = WorldTerrain.Elevation(seed, x + System.Math.Sin(yaw) * distance, z + System.Math.Cos(yaw) * distance);
                    Assert.That(ground, Is.LessThan(eye - distance * System.Math.Tan(2 * System.Math.PI / 180)),
                        "Open view at " + angle + " degrees, " + distance + "m");
                }
            Assert.That(WorldTerrain.Elevation(seed, 0, 20), Is.GreaterThan(WorldTerrain.Elevation(seed, 0, 0) + 1.2f),
                "Regression context: the spawn floor faces rising ground");
            Assert.That(FlightRegions.InDepartureLookoutClearing(x + 9, z), Is.True);
            Assert.That(FlightRegions.InDepartureLookoutClearing(x + 11, z), Is.False);
        }
    }
}
