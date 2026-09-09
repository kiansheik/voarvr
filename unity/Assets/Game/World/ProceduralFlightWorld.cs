using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;

namespace VoarVR.World
{
    // A small deterministic flight course: magical forest to port, spirit city to starboard.
    // It generates once at scene load and reuses shared materials.
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

        private sealed class ThermalGlyph
        {
            public Transform Transform;
            public LineRenderer Line;
            public int Column;
        }

        public void Configure(WindField field, Material city, Material forest, Material canopy, Material spirit, Material visibleWind, Material hazardWind)
        {
            wind = field; cityMaterial = city; forestMaterial = forest; canopyMaterial = canopy; spiritMaterial = spirit;
            windMaterial = visibleWind; hazardWindMaterial = hazardWind;
        }

        private void Awake()
        {
            if (transform.Find("GeneratedLandscape") != null) return;
            var root = new GameObject("GeneratedLandscape").transform;
            root.SetParent(transform, false);
            GenerateCity(root);
            GenerateForest(root);
            GenerateFlightGates(root);
            GenerateWindGlyphs(root);
            GeneratePerchBeacons(root);
        }

        private void Update()
        {
            float time = Time.time;
            for (int i = 0; i < crowns.Count; i++)
            {
                var airflow = wind != null ? wind.Sample(crowns[i].position, time) : Vector3.zero;
                crowns[i].localRotation = Quaternion.Euler(
                    Mathf.Sin(time * 1.1f + i) * (1f + airflow.magnitude * .45f),
                    time * (i % 2 == 0 ? 2f : -2f),
                    -airflow.x * 1.2f);
            }
            bool airVisible = wind == null || wind.Mode != WindMode.StillAir;
            for (int i = 0; i < thermalGlyphs.Count; i++)
            {
                var glyph=thermalGlyphs[i];
                glyph.Line.enabled=airVisible;
                glyph.Line.sharedMaterial=wind != null && wind.Mode == WindMode.Wild && glyph.Column == 2
                    ? hazardWindMaterial : windMaterial;
                if(airVisible) glyph.Transform.Rotate(0f,(12f+i%3*5f)*Time.deltaTime,0f,Space.Self);
            }
            for(int i=0;i<landingRings.Count;i++)
                landingRings[i].Rotate(0f,(10f+i%3*4f)*Time.deltaTime,0f,Space.Self);
        }

        private void GenerateCity(Transform root)
        {
            var city = new GameObject("SpiritCity").transform; city.SetParent(root, false);
            for (int i = 0; i < 14; i++)
            {
                float x = 8f + (i % 3) * 8f + Hash(i, 3) * 3f;
                float z = 18f + i * 12f;
                float h = 5f + Hash(i, 7) * 12f;
                var tower = Primitive($"CityTower_{i:00}", PrimitiveType.Cube, city, cityMaterial,
                    new Vector3(x, h * .5f, z), new Vector3(4.5f, h, 4.5f));
                Primitive($"SpiritRoof_{i:00}", PrimitiveType.Sphere, city, spiritMaterial,
                    new Vector3(x, h + .4f, z), new Vector3(5.3f, .55f, 5.3f));
                if (i == 4 || i == 9)
                {
                    var perch = new GameObject($"CityPerch_{i:00}");
                    perch.transform.SetParent(city, false);
                    perch.transform.position = new Vector3(x, h + .55f, z);
                    perch.AddComponent<PerchPoint>();
                }
            }
        }

        private void GenerateForest(Transform root)
        {
            var forest = new GameObject("SpiritForest").transform; forest.SetParent(root, false);
            for (int i = 0; i < 18; i++)
            {
                float x = -8f - (i % 3) * 8f - Hash(i, 11) * 3f;
                float z = 12f + i * 10f;
                float h = 4f + Hash(i, 17) * 5f;
                var trunk = Primitive($"Tree_{i:00}", PrimitiveType.Cylinder, forest, forestMaterial,
                    new Vector3(x, h * .5f, z), new Vector3(.8f, h * .5f, .8f));
                var crown = Primitive($"SpiritCrown_{i:00}", PrimitiveType.Sphere, forest, canopyMaterial,
                    new Vector3(x, h + 1.2f, z), new Vector3(4.2f, 2.5f, 4.2f));
                crowns.Add(crown.transform);
                if (i == 6 || i == 13)
                {
                    var perch = new GameObject($"ForestPerch_{i:00}");
                    perch.transform.SetParent(forest, false);
                    perch.transform.position = new Vector3(x, h + 1.1f, z);
                    perch.AddComponent<PerchPoint>();
                }
            }
        }

        private void GenerateFlightGates(Transform root)
        {
            var gates = new GameObject("FlightGates").transform; gates.SetParent(root, false);
            for (int i = 0; i < 6; i++)
            {
                float z = 35f + i * 27f;
                float x = i % 2 == 0 ? -1.5f : 2f;
                float y = 5f + (i % 3) * 2.5f;
                Primitive($"Gate_{i:00}_L", PrimitiveType.Cube, gates, spiritMaterial,
                    new Vector3(x - 3.5f, y, z), new Vector3(.35f, 7f, .35f));
                Primitive($"Gate_{i:00}_R", PrimitiveType.Cube, gates, spiritMaterial,
                    new Vector3(x + 3.5f, y, z), new Vector3(.35f, 7f, .35f));
                Primitive($"Gate_{i:00}_Top", PrimitiveType.Cube, gates, spiritMaterial,
                    new Vector3(x, y + 3.5f, z), new Vector3(7.35f, .35f, .35f));
            }
        }

        private void GenerateWindGlyphs(Transform root)
        {
            var glyphRoot = new GameObject("VisibleWind").transform; glyphRoot.SetParent(root, false);
            for (int i = 0; i < WindField.ThermalCenters.Length; i++)
            {
                var center = WindField.ThermalCenters[i];
                for (int strand = 0; strand < 2; strand++)
                {
                    var go = new GameObject($"Thermal_{i}_{strand}");
                    go.transform.SetParent(glyphRoot, false);
                    var line = go.AddComponent<LineRenderer>();
                    line.sharedMaterial = windMaterial;
                    line.useWorldSpace = false; line.loop = false;
                    line.widthMultiplier = .10f; line.positionCount = 48;
                    for (int p = 0; p < line.positionCount; p++)
                    {
                        float t = p / 47f;
                        float a = t * Mathf.PI * 6f + strand * Mathf.PI;
                        line.SetPosition(p, new Vector3(Mathf.Cos(a) * (2.5f + t), t * 13f, Mathf.Sin(a) * (2.5f + t)));
                    }
                    go.transform.position = center;
                    thermalGlyphs.Add(new ThermalGlyph{Transform=go.transform,Line=line,Column=i});
                }
            }
        }

        private void GeneratePerchBeacons(Transform root)
        {
            var beacons = new GameObject("LandingBeacons").transform; beacons.SetParent(root, false);
            var perches = FindObjectsByType<PerchPoint>(FindObjectsInactive.Exclude);
            for (int i = 0; i < perches.Length; i++)
            {
                var go = new GameObject($"LandingRing_{i:00}"); go.transform.SetParent(beacons, false);
                go.transform.position = perches[i].transform.position + Vector3.up * .7f;
                var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = spiritMaterial;
                line.useWorldSpace = false; line.loop = true; line.widthMultiplier = .10f; line.positionCount = 32;
                for (int p = 0; p < line.positionCount; p++)
                {
                    float a = p * Mathf.PI * 2f / line.positionCount;
                    line.SetPosition(p, new Vector3(Mathf.Cos(a) * 1.25f, 0f, Mathf.Sin(a) * 1.25f));
                }
                landingRings.Add(go.transform);
            }
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Material material,
            Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            var renderer = go.GetComponent<Renderer>(); if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
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
