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
            mode = (WindMode)(((int)mode + 1) % 4);
        }

        public void SetMode(WindMode value) => mode = value;
        public bool IsHazardColumn(int index) => mode == WindMode.Wild && index == 2;

        public Vector3 GetThermalCenter(int index, float simulationTime)
        {
            var origin = ThermalCenters[index];
            if (mode == WindMode.StillAir) return origin;
            float range = mode == WindMode.Assisted ? 5f : mode == WindMode.Touring ? 8f : 12f;
            float phase = index * 1.91f;
            return origin + new Vector3(
                range * (.68f * Mathf.Sin(simulationTime * .071f + phase)
                    + .32f * Mathf.Sin(simulationTime * .137f + phase * 2.3f)),
                0f,
                range * (.62f * Mathf.Cos(simulationTime * .059f + phase * 1.4f)
                    + .38f * Mathf.Sin(simulationTime * .113f + phase * .7f)));
        }

        public Vector3 Sample(Vector3 worldPosition, float simulationTime)
        {
            if (mode == WindMode.StillAir) return Vector3.zero;
            float cross = mode == WindMode.Assisted ? .65f : mode == WindMode.Touring ? 1.15f : 2.2f;
            float thermalStrength = mode == WindMode.Assisted ? 6.2f : mode == WindMode.Touring ? 4.8f : 7.2f;
            float turbulence = mode == WindMode.Assisted ? .08f : mode == WindMode.Touring ? .25f : 1.15f;
            var wind = new Vector3(
                cross * Mathf.Sin(worldPosition.z * .027f + simulationTime * .18f),
                0f,
                cross * .22f * Mathf.Cos(worldPosition.x * .08f - simulationTime * .14f));
            for (int i = 0; i < ThermalCenters.Length; i++)
            {
                var center = GetThermalCenter(i, simulationTime);
                var horizontal = new Vector2(worldPosition.x - center.x, worldPosition.z - center.z);
                float radial = Mathf.Exp(-horizontal.sqrMagnitude / 64f);
                float breathing = .88f + .12f * Mathf.Sin(simulationTime * .47f + i * 1.7f);
                float vertical = thermalStrength * radial * breathing;
                if (IsHazardColumn(i)) vertical *= -0.7f;
                wind.y += vertical;
                float swirl = radial * (mode == WindMode.Wild ? 1.4f : .45f);
                wind.x += -horizontal.y * swirl * .035f;
                wind.z += horizontal.x * swirl * .035f;
                // Gentle convergence helps a circling player remain in the usable core.
                wind.x += -horizontal.x * radial * .025f;
                wind.z += -horizontal.y * radial * .025f;
            }
            wind += turbulence * new Vector3(
                Mathf.Sin(simulationTime * 1.7f + worldPosition.z * .11f),
                .35f * Mathf.Sin(simulationTime * 1.3f + worldPosition.x * .19f),
                Mathf.Cos(simulationTime * 1.1f + worldPosition.x * .09f));
            return wind;
        }
    }
}
