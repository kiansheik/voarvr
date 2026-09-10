using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    // Serialized scene/material bridge. World lifetime belongs to the pooled streamer.
    public sealed class ProceduralFlightWorld : MonoBehaviour
    {
        [SerializeField] private WindField wind;
        [SerializeField] private Material cityMaterial;
        [SerializeField] private Material forestMaterial;
        [SerializeField] private Material canopyMaterial;
        [SerializeField] private Material spiritMaterial;
        [SerializeField] private Material windMaterial;
        [SerializeField] private Material hazardWindMaterial;
        public WorldStreamer Streamer { get; private set; }
        public Material LandingMaterial => spiritMaterial;
        public Material AirflowMaterial => windMaterial;
        public void Configure(WindField field, Material city, Material forest, Material canopy, Material spirit, Material visibleWind, Material hazardWind)
        {
            wind = field; cityMaterial = city; forestMaterial = forest; canopyMaterial = canopy; spiritMaterial = spirit;
            windMaterial = visibleWind; hazardWindMaterial = hazardWind;
        }
        private void Awake()
        {
            var space = GetComponent<WorldSpace>(); if (space == null) space = gameObject.AddComponent<WorldSpace>();
            Streamer = GetComponent<WorldStreamer>(); if (Streamer == null) Streamer = gameObject.AddComponent<WorldStreamer>();
            Streamer.Configure(space, forestMaterial, cityMaterial, forestMaterial, canopyMaterial);
            gameObject.AddComponent<SkyArchipelago>().Configure(space);
            gameObject.AddComponent<SkyWeatherPresentation>().Configure(space);
            if (wind != null)
            {
                wind.Configure(space);
                var seeds=new GameObject("Thermal seeds");seeds.transform.SetParent(transform,false);seeds.AddComponent<ThermalSeeds>().Configure(wind,space,FindAnyObjectByType<BirdFlightDriver>());
                var visible = gameObject.AddComponent<WindVisualizer>();
                visible.Configure(wind, space, FindAnyObjectByType<BirdFlightDriver>(), windMaterial, hazardWindMaterial);
            }
            // Legacy corridor floor/perches must not coexist with real streamed terrain.
            foreach (var perch in FindObjectsByType<PerchPoint>(FindObjectsInactive.Exclude)) perch.gameObject.SetActive(false);
            var oldGround = GameObject.Find("Ground"); if (oldGround != null) oldGround.SetActive(false);
        }
    }
}
