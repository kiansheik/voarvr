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
    }
}
