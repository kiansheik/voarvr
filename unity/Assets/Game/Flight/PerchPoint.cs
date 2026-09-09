using UnityEngine;

namespace VoarVR.Flight
{
    // Marker only: lets BirdFlightDriver find perch transforms in the scene without a
    // collider/trigger. Perching is a plain distance/speed check in BirdFlightController.
    public sealed class PerchPoint : MonoBehaviour
    {
    }
}
