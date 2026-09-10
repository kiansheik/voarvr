using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;

namespace VoarVR.World
{
    public sealed class WorldChunk : MonoBehaviour
    {
        public long X { get; private set; }
        public long Z { get; private set; }
        public bool Ready { get; private set; }
        public int GenerationStage { get; private set; }
        public int ColliderProxyCount => boxes.Count + (terrainCollider != null ? 1 : 0);
        public int EnabledColliderCount
        {
            get { int count = terrainCollider != null && terrainCollider.enabled ? 1 : 0; foreach (var box in boxes) if (box.enabled) count++; return count; }
        }
        public int VertexCount
        {
            get { int count = 0; foreach (var mesh in meshes) if (mesh != null) count += mesh.vertexCount; return count; }
        }
        private static readonly int[] Faces = { 0,3,2,1,4,5,6,7,0,1,5,4,1,2,6,5,2,3,7,6,3,0,4,7 };
        private readonly Vector3[] corners = new Vector3[8];
        private const int Grid = 16;
        private readonly List<Vector3>[] vertices = new List<Vector3>[4];
        private readonly List<int>[] triangles = new List<int>[4];
        private readonly Mesh[] meshes = new Mesh[4];
        private readonly List<BoxCollider> boxes = new List<BoxCollider>();
        private readonly List<Vector3> boxCenters = new List<Vector3>();
        private readonly List<Vector3> boxSizes = new List<Vector3>();
        private MeshCollider terrainCollider;
        private Mesh terrain;
        private Vector3[] groundVertices, groundNormals;
        private int[] groundTriangles;
        private int seed, row, feature, boxCount;
        private bool collisionWanted;

        public void Initialize(Material[] materials)
        {
            gameObject.layer = WorldStreamer.CollisionLayer;
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = new List<Vector3>(1600); triangles[i] = new List<int>(2400);
                var go = new GameObject(new[] { "Terrain", "SpiritCityAndWater", "Forest", "Canopy" }[i]);
                go.transform.SetParent(transform, false); go.layer = WorldStreamer.CollisionLayer;
                meshes[i] = new Mesh { name = "Reusable chunk " + i }; meshes[i].MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials[i];
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (i == 0) { terrain = meshes[i]; terrainCollider = go.AddComponent<MeshCollider>(); go.AddComponent<LandingSurface>(); }
            }
            gameObject.AddComponent<LandingSurface>();
            groundNormals = new Vector3[(Grid + 1) * (Grid + 1)];
            groundVertices = new Vector3[(Grid + 1) * (Grid + 1)]; groundTriangles = new int[Grid * Grid * 6];
            int t = 0;
            for (int z = 0; z < Grid; z++) for (int x = 0; x < Grid; x++)
            {
                int a = z * (Grid + 1) + x;
                groundTriangles[t++] = a; groundTriangles[t++] = a + Grid + 1; groundTriangles[t++] = a + 1;
                groundTriangles[t++] = a + 1; groundTriangles[t++] = a + Grid + 1; groundTriangles[t++] = a + Grid + 2;
            }
        }
        public void Begin(int worldSeed, long x, long z, Vector3 localOrigin)
        {
            seed = worldSeed; X = x; Z = z; transform.position = localOrigin;
            GetComponent<LandingSurface>().SurfaceId = unchecked((int)WorldTerrain.Hash(seed, x, z, 900)) | 1;
            terrainCollider.GetComponent<LandingSurface>().SurfaceId = unchecked((int)WorldTerrain.Hash(seed, x, z, 901)) | 1;
            Ready = false; GenerationStage = row = feature = boxCount = 0;
            boxCenters.Clear(); boxSizes.Clear(); collisionWanted = false;
            terrainCollider.enabled = false; terrainCollider.sharedMesh = null;
            foreach (var box in boxes) box.enabled = false;
            for (int i = 0; i < 4; i++) { vertices[i].Clear(); triangles[i].Clear(); meshes[i].Clear(); }
            gameObject.SetActive(true);
        }
        // One small work item: one row, one feature cell, or one bounded mesh upload/cook.
        public bool GenerateStep()
        {
            if (Ready) return true;
            if (GenerationStage == 0)
            {
                for (int x = 0; x <= Grid; x++)
                {
                    float lx = x * 8, lz = row * 8;
                    double wx = X * 128d + lx, wz = Z * 128d + lz;
                    groundVertices[row * (Grid + 1) + x] = new Vector3(lx, WorldTerrain.Elevation(seed, wx, wz), lz);
                    groundNormals[row * (Grid + 1) + x] = new Vector3(
                        WorldTerrain.Elevation(seed, wx - 1, wz) - WorldTerrain.Elevation(seed, wx + 1, wz), 2,
                        WorldTerrain.Elevation(seed, wx, wz - 1) - WorldTerrain.Elevation(seed, wx, wz + 1)).normalized;
                }
                if (row < Grid) RiverSegment(row);
                if (++row > Grid) GenerationStage++;
            }
            else if (GenerationStage == 1)
            {
                terrain.vertices = groundVertices; terrain.triangles = groundTriangles; terrain.normals = groundNormals; terrain.RecalculateBounds();
                GenerationStage++;
            }
            else if (GenerationStage == 2)
            {
                GenerateCell(feature++);
                if (feature == 36) GenerationStage++;
            }
            else if (GenerationStage < 6)
            {
                int i = GenerationStage - 2;
                meshes[i].SetVertices(vertices[i]); meshes[i].SetTriangles(triangles[i], 0);
                meshes[i].RecalculateNormals(); meshes[i].RecalculateBounds(); GenerationStage++;
            }
            else { Ready = true; SetCollision(collisionWanted); }
            return Ready;
        }
        private float MeshHeight(float x, float z)
        {
            int ix = Mathf.Min(Grid - 1, Mathf.FloorToInt(x / 8)), iz = Mathf.Min(Grid - 1, Mathf.FloorToInt(z / 8));
            float fx = x / 8 - ix, fz = z / 8 - iz;
            float a = WorldTerrain.Elevation(seed, X * 128d + ix * 8, Z * 128d + iz * 8);
            float b = WorldTerrain.Elevation(seed, X * 128d + (ix + 1) * 8, Z * 128d + iz * 8);
            float c = WorldTerrain.Elevation(seed, X * 128d + ix * 8, Z * 128d + (iz + 1) * 8);
            if (fx + fz <= 1) return a + (b - a) * fx + (c - a) * fz;
            float d = WorldTerrain.Elevation(seed, X * 128d + (ix + 1) * 8, Z * 128d + (iz + 1) * 8);
            return d + (c - d) * (1 - fx) + (b - d) * (1 - fz);
        }
        private void RiverSegment(int segment)
        {
            float z0 = segment * 8, z1 = z0 + 8;
            float c0 = (float)(WorldTerrain.RiverCenter(seed, Z * 128d + z0) - X * 128d);
            float c1 = (float)(WorldTerrain.RiverCenter(seed, Z * 128d + z1) - X * 128d);
            if ((c0 < -5 && c1 < -5) || (c0 > 133 && c1 > 133)) return;
            float a0 = Mathf.Clamp(c0 - 5, 0, 128), b0 = Mathf.Clamp(c0 + 5, 0, 128);
            float a1 = Mathf.Clamp(c1 - 5, 0, 128), b1 = Mathf.Clamp(c1 + 5, 0, 128);
            var v = vertices[1]; var t = triangles[1]; int start = v.Count;
            v.Add(new Vector3(a0, MeshHeight(a0, z0) + .08f, z0));
            v.Add(new Vector3(a1, MeshHeight(a1, z1) + .08f, z1));
            v.Add(new Vector3(b0, MeshHeight(b0, z0) + .08f, z0));
            v.Add(new Vector3(b1, MeshHeight(b1, z1) + .08f, z1));
            t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start+2);t.Add(start+1);t.Add(start+3);
        }
        private void GenerateCell(int cell)
        {
            long gx = X * 6 + cell % 6, gz = Z * 6 + cell / 6;
            float x = (cell % 6 + .2f + WorldTerrain.Unit(seed, gx, gz, 1) * .6f) * (128f / 6);
            float z = (cell / 6 + .2f + WorldTerrain.Unit(seed, gx, gz, 2) * .6f) * (128f / 6);
            double wx = X * 128d + x, wz = Z * 128d + z;
            if (wx * wx + wz * wz < 225) return; // reliable clear departure area
            float y = WorldTerrain.Elevation(seed, wx, wz), river = WorldTerrain.RiverDistance(seed, wx, wz);
            if (river < 8) return;
            float biome = WorldTerrain.Biome(seed, wx, wz);
            float variety = WorldTerrain.Unit(seed, gx, gz, 3);
            if (biome > .64f && variety > .17f)
            {
                float h = 7 + variety * 23, w = 5 + WorldTerrain.Unit(seed, gx, gz, 4) * 4;
                Box(1, new Vector3(x, y + h / 2, z), new Vector3(w, h, w));
                Box(3, new Vector3(x, y + h + .2f, z), new Vector3(w + 1, .4f, w + 1));
                Box(1, new Vector3(x, y + h + 1.5f, z), new Vector3(w * .65f, 2.2f, w * .65f));
            }
            else if (biome < .57f && variety > .22f)
            {
                float h = 5 + variety * 8, w = 4 + variety * 4;
                Box(2, new Vector3(x, y + h / 2, z), new Vector3(.8f, h, .8f));
                Box(3, new Vector3(x, y + h, z), new Vector3(w, 2.5f, w * .8f));
                Box(3, new Vector3(x - w * .12f, y + h + 1.8f, z), new Vector3(w * .6f, 1.5f, w * .6f));
            }
            else if (variety > .75f)
                Box(2, new Vector3(x, y + 1, z), new Vector3(7, 2, 5));
        }
        private void Box(int material, Vector3 center, Vector3 size)
        {
            // Independent face vertices preserve readable flat shaded edges.
            var v = vertices[material]; var t = triangles[material];
            Vector3 h = size * .5f;
            corners[0] = new Vector3(-h.x, -h.y, -h.z); corners[1] = new Vector3(h.x, -h.y, -h.z);
            corners[2] = new Vector3(h.x, -h.y, h.z); corners[3] = new Vector3(-h.x, -h.y, h.z);
            corners[4] = new Vector3(-h.x, h.y, -h.z); corners[5] = new Vector3(h.x, h.y, -h.z);
            corners[6] = new Vector3(h.x, h.y, h.z); corners[7] = new Vector3(-h.x, h.y, h.z);
            for(int f=0;f<6;f++) { int a=v.Count; for(int p=0;p<4;p++)v.Add(center+corners[Faces[f*4+p]]); t.Add(a);t.Add(a+2);t.Add(a+1);t.Add(a);t.Add(a+3);t.Add(a+2); }
            boxCenters.Add(center); boxSizes.Add(size);
            if (boxes.Count < boxCenters.Count) boxes.Add(gameObject.AddComponent<BoxCollider>());
            var collider = boxes[boxCenters.Count - 1]; collider.enabled = false; collider.center = center; collider.size = size;
            boxCount = boxCenters.Count;
        }
        public void SetCollision(bool enabled)
        {
            collisionWanted = enabled;
            // Enable before binding: activating a collider whose mesh was cleared
            // while pooled can discard a binding assigned while disabled.
            // A Ready/enabled collider with no sharedMesh is not collision-ready.
            terrainCollider.enabled = enabled && Ready;
            if (enabled && Ready && terrainCollider.sharedMesh != terrain)
                terrainCollider.sharedMesh = terrain;
            if (enabled && Ready)
            {
                boxCount = boxCenters.Count;
                for (int i = 0; i < boxCount; i++) { boxes[i].center = boxCenters[i]; boxes[i].size = boxSizes[i]; }
            }
            for (int i = 0; i < boxes.Count; i++) boxes[i].enabled = enabled && Ready && i < boxCount;
        }
        public bool TryGetGround(Vector3 position, out Vector3 point, out Vector3 normal, out int surfaceId)
        {
            point = normal = default; surfaceId = 0;
            if (!Ready || !terrainCollider.enabled || !gameObject.activeInHierarchy) return false;
            Vector3 local = transform.InverseTransformPoint(position);
            if(local.x < 0 || local.z < 0 || local.x > 128 || local.z > 128) return false;
            int x = Mathf.Min(Grid - 1, Mathf.FloorToInt(local.x / 8));
            int z = Mathf.Min(Grid - 1, Mathf.FloorToInt(local.z / 8));
            int a = z * (Grid + 1) + x;
            Vector3 v0, v1, v2;
            if(local.x / 8 - x + local.z / 8 - z <= 1)
            { v0=groundVertices[a]; v1=groundVertices[a+Grid+1]; v2=groundVertices[a+1]; }
            else
            { v0=groundVertices[a+1]; v1=groundVertices[a+Grid+1]; v2=groundVertices[a+Grid+2]; }
            var n=Vector3.Cross(v1-v0,v2-v0).normalized;
            float y=v0.y-(n.x*(local.x-v0.x)+n.z*(local.z-v0.z))/n.y;
            point=transform.TransformPoint(new Vector3(local.x,y,local.z));
            normal=transform.TransformDirection(n).normalized;
            surfaceId=terrainCollider.GetComponent<LandingSurface>().SurfaceId;
            return true;
        }
        private void OnDestroy() { foreach (var mesh in meshes) if (mesh != null) Destroy(mesh); }
    }
}
