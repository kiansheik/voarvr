using System;
using UnityEngine;

namespace VoarVR.World
{
    // Global double-coordinate fields, never a per-chunk random biome roll.
    public static class WorldTerrain
    {
        public const int ChunkSize = 128;
        public static double RiverCenter(int seed, double z) => 12 + 21 * Math.Sin(z / 180 + seed * .001) + 9 * Math.Sin(z / 73);
        public static float RiverDistance(int seed, double x, double z) => (float)Math.Abs(x - RiverCenter(seed, z));
        public static float Biome(int seed, double x, double z) => (float)(.5 + .32 * Math.Sin(x / 230 + seed * .003) + .18 * Math.Sin(z / 310 - x / 450));
        public static float Elevation(int seed, double x, double z)
        {
            double valley = 2 + 2 * Math.Sin(x / 170) * Math.Sin(z / 210);
            double ridge = Math.Pow(.5 + .5 * Math.Sin(x / 130 + z / 290 + seed * .001), 4) * 23;
            double river = Math.Min(1, RiverDistance(seed, x, z) / 28);
            double clearing = Math.Min(1, Math.Sqrt(x * x + z * z) / 85);
            return (float)((valley + ridge * river) * clearing - 1);
        }
        public static uint Hash(int seed, long x, long z, int salt = 0)
        {
            unchecked
            {
                // Mix each signed coordinate in sequence. XORing independent signed
                // products admits reflection collisions such as (-1,1) and (1,-1).
                ulong n = (uint)seed + 0x9e3779b97f4a7c15UL;
                n = Mix(n + (ulong)x);
                n = Mix(n + (ulong)z + 0xbf58476d1ce4e5b9UL);
                n = Mix(n + (uint)salt);
                return (uint)(n ^ (n >> 32));
            }
        }
        private static ulong Mix(ulong n)
        {
            unchecked
            {
                n = (n ^ (n >> 30)) * 0xbf58476d1ce4e5b9UL;
                n = (n ^ (n >> 27)) * 0x94d049bb133111ebUL;
                return n ^ (n >> 31);
            }
        }
        public static float Unit(int seed, long x, long z, int salt) => (Hash(seed, x, z, salt) & 0xffffff) / 16777215f;
    }
}
