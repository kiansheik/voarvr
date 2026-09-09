using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;

namespace VoarVR.World
{
    // A bounded course, generated once. Geometry and wind paths reuse shared materials.
    public sealed class ProceduralFlightWorld : MonoBehaviour
    {
        [SerializeField] private WindField wind;
        [SerializeField] private Material cityMaterial;
        [SerializeField] private Material forestMaterial;
        [SerializeField] private Material canopyMaterial;
        [SerializeField] private Material spiritMaterial;
        [SerializeField] private Material windMaterial;
        [SerializeField] private Material hazardWindMaterial;
        private readonly List<Transform> crowns = new List<Transform>();
        private readonly List<ThermalGlyph> thermalGlyphs = new List<ThermalGlyph>();
        private readonly List<Transform> landingRings = new List<Transform>();
        private readonly List<LineRenderer> travelingDrafts = new List<LineRenderer>();
        private BirdFlightDriver driver;
        private Mesh riverMesh;
        private MaterialPropertyBlock flowProperties;
        private static readonly int WindTimeId = Shader.PropertyToID("_FlightWindTime");
        private static readonly int FlowDirectionId = Shader.PropertyToID("_FlowDirection");

        private sealed class ThermalGlyph
        {
            public Transform Transform;
            public LineRenderer Line;
            public int Column;
            public int Strand;
        }

        public void Configure(WindField field, Material city, Material forest, Material canopy, Material spirit, Material visibleWind, Material hazardWind)
        {
            wind = field; cityMaterial = city; forestMaterial = forest; canopyMaterial = canopy; spiritMaterial = spirit;
            windMaterial = visibleWind; hazardWindMaterial = hazardWind;
        }

        private void Awake()
        {
            driver = FindAnyObjectByType<BirdFlightDriver>();
            flowProperties = new MaterialPropertyBlock();
            if (transform.Find("GeneratedLandscape") != null) return;
            var root = new GameObject("GeneratedLandscape").transform;
            root.SetParent(transform, false);
            GenerateTerrain(root);
            GenerateCity(root);
            GenerateForest(root);
            GenerateFlightGates(root);
            GenerateWindGlyphs(root);
            GeneratePerchBeacons(root);
        }

        private void LateUpdate()
        {
            // Render the final simulation instant, including pause/reset. No separate wall
            // clock, incremental rotations or particle history can drift from flight physics.
            float time = driver != null && driver.Controller != null ? driver.Controller.SimulationTime : 0f;
            Shader.SetGlobalFloat(WindTimeId, time);
            for (int i = 0; i < crowns.Count; i++)
            {
                var airflow = wind != null ? wind.Sample(crowns[i].position, time) : Vector3.zero;
                crowns[i].localRotation = Quaternion.Euler(
                    Mathf.Sin(time * 1.1f + i) * (1f + airflow.magnitude * .45f),
                    time * (i % 2 == 0 ? 2f : -2f), -airflow.x * 1.2f);
            }
            bool airVisible = wind != null && wind.Mode != WindMode.StillAir;
            for (int i = 0; i < thermalGlyphs.Count; i++)
            {
                var glyph = thermalGlyphs[i];
                glyph.Line.enabled = airVisible;
                if (!airVisible) continue;
                bool hazard = wind.IsHazardColumn(glyph.Column);
                glyph.Line.sharedMaterial = hazard ? hazardWindMaterial : windMaterial;
                flowProperties.SetFloat(FlowDirectionId, hazard ? -1f : 1f);
                glyph.Line.SetPropertyBlock(flowProperties);
                glyph.Transform.position = wind.GetThermalCenter(glyph.Column, time);
                // Monotonic height: never wrap individual vertices through the column.
                // The pulse travels along this continuous tapered curl in the flow direction.
                for (int p = 0; p < glyph.Line.positionCount; p++)
                {
                    float t = p / (float)(glyph.Line.positionCount - 1);
                    float angle = t * Mathf.PI * 2.6f + glyph.Strand * Mathf.PI * 2f / 3f
                        + .18f * Mathf.Sin(time * .23f + glyph.Column);
                    float radius = 2.1f + t * 1.8f + .22f * Mathf.Sin(t * Mathf.PI * 2f + time * .3f);
                    glyph.Line.SetPosition(p, new Vector3(Mathf.Cos(angle) * radius, t * 15f, Mathf.Sin(angle) * radius));
                }
            }
            for (int i = 0; i < travelingDrafts.Count; i++)
            {
                var line = travelingDrafts[i];
                line.enabled = airVisible;
                if (airVisible) TraceDraft(line, i, time);
            }
            for (int i = 0; i < landingRings.Count; i++)
                landingRings[i].localRotation = Quaternion.Euler(0f, time * (10f + i % 3 * 4f), 0f);
        }

        private void TraceDraft(LineRenderer line, int index, float time)
        {
            // Deterministic short pathline: seed, advect with Sample, then draw the final
            // three seconds of air motion. Fading the complete line hides seed recycling;
            // no segment ever connects across a wrap or follows an invented fast trajectory.
            const float lifetime = 10f;
            float phase = index * 1.61f;
            float birth = Mathf.Floor((time + phase) / lifetime) * lifetime - phase;
            float age = time - birth;
            int column = index % WindField.ThermalCenters.Length;
            bool hazard = wind.IsHazardColumn(column);
            line.sharedMaterial = hazard ? hazardWindMaterial : windMaterial;
            float angle = index * 2.4f + birth * .07f;
            var point = wind.GetThermalCenter(column, birth)
                + new Vector3(Mathf.Cos(angle) * 5f, hazard ? 16f : 1f, Mathf.Sin(angle) * 5f);
            float tailTime = Mathf.Max(0f, age - 3f);
            const int leadSteps = 12;
            float leadDt = tailTime / leadSteps;
            for (int step = 0; step < leadSteps; step++)
                point = Advect(point, birth + step * leadDt, leadDt);
            float pathDt = (age - tailTime) / (line.positionCount - 1);
            for (int p = 0; p < line.positionCount; p++)
            {
                line.SetPosition(p, point);
                point = Advect(point, birth + tailTime + p * pathDt, pathDt);
            }
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(age, lifetime - age) / .8f);
            line.startColor = line.endColor = new Color(1f, 1f, 1f, fade);
        }

        private Vector3 Advect(Vector3 point, float time, float dt)
        {
            if (dt <= 0f) return point;
            // Midpoint integration keeps the displayed path tangent to the actual field.
            var midpoint = point + wind.Sample(point, time) * (dt * .5f);
            return point + wind.Sample(midpoint, time + dt * .5f) * dt;
        }

        private void GenerateTerrain(Transform root)
        {
            var terrain = new GameObject("LandscapeContours").transform;
            terrain.SetParent(root, false);
            // Low distant ridges stay outside the clear flight corridor. They are visual
            // silhouettes only, not a second terrain/contact simulation.
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Primitive($"DistantRidge_{i:00}", PrimitiveType.Sphere, terrain, forestMaterial,
                    new Vector3(side * (48f + i % 3 * 4f), -5f, 35f + i / 2 * 78f),
                    new Vector3(42f + i % 2 * 7f, 30f + i % 3 * 4f, 88f));
            }
            // One horizontal opaque ribbon gives the flat ground a winding river without
            // transparent water, reflections, colliders or dozens of extra renderers.
            const int segments = 48;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (int p = 0; p <= segments; p++)
            {
                float z = -20f + p * 6f;
                float x = 1f + Mathf.Sin(z * .036f) * 3f;
                vertices[p * 2] = new Vector3(x - 1.15f, .035f, z);
                vertices[p * 2 + 1] = new Vector3(x + 1.15f, .035f, z);
                if (p == segments) continue;
                int v = p * 2, t = p * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            riverMesh = new Mesh { name = "GeneratedRiverRibbon", vertices = vertices, triangles = triangles };
            riverMesh.RecalculateBounds();
            riverMesh.RecalculateNormals();
            var river = new GameObject("WindingRiver"); river.transform.SetParent(terrain, false);
            river.AddComponent<MeshFilter>().sharedMesh = riverMesh;
            var renderer = river.AddComponent<MeshRenderer>(); renderer.sharedMaterial = cityMaterial;
            ConfigureRenderer(renderer);
        }

        private void OnDestroy()
        {
            if (riverMesh != null) Destroy(riverMesh);
        }

        private void GenerateCity(Transform root)
        {
            var city = new GameObject("SpiritCity").transform; city.SetParent(root, false);
            for (int i = 0; i < 14; i++)
            {
                float x = 8f + i % 3 * 8f + Hash(i, 3) * 3f;
                float z = 18f + i * 12f;
                float h = 5f + Hash(i, 7) * 12f;
                float width = 3.6f + Hash(i, 19) * 1.5f;
                Primitive($"CityTower_{i:00}", PrimitiveType.Cube, city, cityMaterial,
                    new Vector3(x, h * .5f, z), new Vector3(width, h, width));
                // Two stepped terraces replace identical floating disks. Cube meshes are
                // shared; only fourteen extra small roof renderers are introduced.
                Primitive($"SpiritRoof_{i:00}", PrimitiveType.Cube, city, spiritMaterial,
                    new Vector3(x, h + .12f, z), new Vector3(width + .6f, .24f, width + .6f));
                Primitive($"RoofTier_{i:00}", PrimitiveType.Cube, city, cityMaterial,
                    new Vector3(x, h + .6f, z), new Vector3(width * .65f, .72f, width * .65f));
                if (i == 4 || i == 9)
                {
                    var perch = new GameObject($"CityPerch_{i:00}"); perch.transform.SetParent(city, false);
                    perch.transform.position = new Vector3(x, h + .96f, z);
                    perch.AddComponent<PerchPoint>();
                }
            }
        }

        private void GenerateForest(Transform root)
        {
            var forest = new GameObject("SpiritForest").transform; forest.SetParent(root, false);
            for (int i = 0; i < 18; i++)
            {
                float x = -8f - i % 3 * 8f - Hash(i, 11) * 3f;
                float z = 12f + i * 10f;
                float h = 4f + Hash(i, 17) * 5f;
                Primitive($"Tree_{i:00}", PrimitiveType.Cylinder, forest, forestMaterial,
                    new Vector3(x, h * .5f, z), new Vector3(.8f, h * .5f, .8f));
                float width = 3.2f + Hash(i, 23) * 1.9f;
                float height = i % 3 == 0 ? 3.8f : 1.9f + Hash(i, 29) * 1.1f;
                var crown = Primitive($"SpiritCrown_{i:00}", PrimitiveType.Sphere, forest, canopyMaterial,
                    new Vector3(x, h + height * .35f, z), new Vector3(width, height, width * .8f));
                crowns.Add(crown.transform);
                if (i % 4 == 0)
                {
                    var smaller = Primitive($"CrownLobe_{i:00}", PrimitiveType.Sphere, forest, canopyMaterial,
                        new Vector3(x + 1.2f, h + .5f, z - .6f), new Vector3(width * .65f, height * .7f, width * .6f));
                    crowns.Add(smaller.transform);
                }
                if (i == 6 || i == 13)
                {
                    // Exposed side branch/platform, outside the crown's maximum radius.
                    float platformX = x + width * .5f + 1.1f;
                    var platform = Primitive($"ForestPerch_{i:00}", PrimitiveType.Cube, forest, spiritMaterial,
                        new Vector3(platformX, h - .3f, z), new Vector3(1.8f, .18f, 1.3f));
                    platform.AddComponent<PerchPoint>();
                    Primitive($"PerchBranch_{i:00}", PrimitiveType.Cube, forest, forestMaterial,
                        new Vector3((x + platformX) * .5f, h - .5f, z), new Vector3(platformX - x, .3f, .35f));
                }
            }
        }

        private void GenerateFlightGates(Transform root)
        {
            var gates = new GameObject("FlightGates").transform; gates.SetParent(root, false);
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject($"Gate_{i:00}_Arch"); go.transform.SetParent(gates, false);
                go.transform.position = new Vector3(i % 2 == 0 ? -1.5f : 2f, 5f + i % 3 * 2.5f, 35f + i * 27f);
                var line = go.AddComponent<LineRenderer>();
                ConfigureLine(line, spiritMaterial, 22, .28f, false);
                line.SetPosition(0, new Vector3(-3.5f, -3.5f, 0f));
                for (int p = 0; p < 20; p++)
                {
                    float angle = Mathf.PI - p / 19f * Mathf.PI;
                    line.SetPosition(p + 1, new Vector3(Mathf.Cos(angle) * 3.5f, Mathf.Sin(angle) * 3.5f, 0f));
                }
                line.SetPosition(21, new Vector3(3.5f, -3.5f, 0f));
            }
        }

        private void GenerateWindGlyphs(Transform root)
        {
            var glyphRoot = new GameObject("VisibleWind").transform; glyphRoot.SetParent(root, false);
            for (int i = 0; i < WindField.ThermalCenters.Length; i++)
                for (int strand = 0; strand < 3; strand++)
                {
                    var go = new GameObject($"Thermal_{i}_{strand}"); go.transform.SetParent(glyphRoot, false);
                    var line = go.AddComponent<LineRenderer>();
                    ConfigureLine(line, windMaterial, 48, .13f, true);
                    go.transform.position = WindField.ThermalCenters[i];
                    thermalGlyphs.Add(new ThermalGlyph { Transform = go.transform, Line = line, Column = i, Strand = strand });
                }
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject($"TravelingDraft_{i:00}"); go.transform.SetParent(glyphRoot, false);
                var line = go.AddComponent<LineRenderer>();
                ConfigureLine(line, windMaterial, 24, .10f, true);
                line.useWorldSpace = true;
                travelingDrafts.Add(line);
            }
        }

        private void GeneratePerchBeacons(Transform root)
        {
            var beacons = new GameObject("LandingBeacons").transform; beacons.SetParent(root, false);
            var perches = FindObjectsByType<PerchPoint>(FindObjectsInactive.Exclude);
            for (int i = 0; i < perches.Length; i++)
            {
                var go = new GameObject($"LandingRing_{i:00}"); go.transform.SetParent(beacons, false);
                var surface = perches[i].GetComponent<Renderer>();
                float top = surface != null ? surface.bounds.max.y : perches[i].transform.position.y;
                go.transform.position = new Vector3(perches[i].transform.position.x, top + .45f, perches[i].transform.position.z);
                var line = go.AddComponent<LineRenderer>();
                ConfigureLine(line, spiritMaterial, 32, .10f, false);
                line.loop = true;
                for (int p = 0; p < line.positionCount; p++)
                {
                    float a = p * Mathf.PI * 2f / line.positionCount;
                    line.SetPosition(p, new Vector3(Mathf.Cos(a) * 1.25f, 0f, Mathf.Sin(a) * 1.25f));
                }
                landingRings.Add(go.transform);
            }
        }

        private static void ConfigureLine(LineRenderer line, Material material, int points, float width, bool tapered)
        {
            line.sharedMaterial = material; line.useWorldSpace = false; line.loop = false;
            line.positionCount = points; line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Stretch;
            if (tapered) line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(.15f, 1f),
                new Keyframe(.8f, .8f), new Keyframe(1f, 0f));
            ConfigureRenderer(line);
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Material material,
            Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            var renderer = go.GetComponent<Renderer>(); if (material != null) renderer.sharedMaterial = material;
            ConfigureRenderer(renderer);
            return go;
        }

        private static float Hash(int value, int salt)
        {
            uint x = (uint)value * 747796405u + (uint)salt * 2891336453u;
            x = (x >> ((int)(x >> 28) + 4)) ^ x;
            return (x * 277803737u >> 22) / 1023f;
        }
    }
}
