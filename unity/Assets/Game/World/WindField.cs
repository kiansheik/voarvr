using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    public enum WindMode { Assisted, Touring, Wild, StillAir }

    // Deterministic, allocation-free wind sampled by both flight and presentation.
    public sealed class WindField : MonoBehaviour, IWindField
    {
        public static readonly Vector3[] ThermalCenters =
        {
            new Vector3(-10f, 0f, 38f),
            new Vector3(12f, 0f, 82f),
            new Vector3(-7f, 0f, 128f),
            new Vector3(14f, 0f, 170f)
        };

        [SerializeField] private WindMode mode = WindMode.Assisted;
        public WindMode Mode => mode;
        public string ModeName => mode switch
        {
            WindMode.Assisted => "Assisted thermals",
            WindMode.Touring => "Touring breeze",
            WindMode.Wild => "Wild weather",
            _ => "Still air / hard"
        };

        public void CycleMode()
        {
            mode = (WindMode)(((int)mode + 1) % System.Enum.GetValues(typeof(WindMode)).Length);
        }

        public void SetMode(WindMode value) => mode = value;

        public Vector3 Sample(Vector3 worldPosition, float simulationTime)
        {
            if (mode == WindMode.StillAir) return Vector3.zero;
            float cross = mode == WindMode.Assisted ? .35f : mode == WindMode.Touring ? .8f : 1.8f;
            float thermalStrength = mode == WindMode.Assisted ? 4.2f : mode == WindMode.Touring ? 2.8f : 4.8f;
            float turbulence = mode == WindMode.Assisted ? .08f : mode == WindMode.Touring ? .25f : 1.15f;
            var wind = new Vector3(
                cross * Mathf.Sin(worldPosition.z * .027f + simulationTime * .18f),
                0f,
                cross * .22f * Mathf.Cos(worldPosition.x * .08f - simulationTime * .14f));
            for (int i = 0; i < ThermalCenters.Length; i++)
            {
                var horizontal = new Vector2(worldPosition.x - ThermalCenters[i].x, worldPosition.z - ThermalCenters[i].z);
                float radial = Mathf.Exp(-horizontal.sqrMagnitude / 38f);
                float vertical = thermalStrength * radial;
                if (mode == WindMode.Wild && i == 2) vertical *= -0.7f;
                wind.y += vertical;
                float swirl = radial * (mode == WindMode.Wild ? 1.4f : .45f);
                wind.x += -horizontal.y * swirl * .035f;
                wind.z += horizontal.x * swirl * .035f;
            }
            wind += turbulence * new Vector3(
                Mathf.Sin(simulationTime * 1.7f + worldPosition.z * .11f),
                .35f * Mathf.Sin(simulationTime * 1.3f + worldPosition.x * .19f),
                Mathf.Cos(simulationTime * 1.1f + worldPosition.x * .09f));
            return wind;
        }
    }
}
