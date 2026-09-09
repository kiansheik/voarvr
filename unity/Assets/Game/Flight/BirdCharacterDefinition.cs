using UnityEngine;

namespace VoarVR.Flight
{
    // Kart-style stat card: designers tune five sliders per species; BuildProfile() bakes them
    // into a BirdFlightProfile, so the existing lift/drag/stall simulation still does the rest
    // of the work from body shape and wind. Size=1/Speed=.5/Power=.5/Agility=.5/Weight=.5
    // reproduces BirdFlightProfile.Duck() exactly, so Duck is the zero point every other
    // species is tuned relative to.
    [CreateAssetMenu(menuName = "VoarVR/Bird Character", fileName = "BirdCharacter")]
    public sealed class BirdCharacterDefinition : ScriptableObject
    {
        public string DisplayName = "Duck";
        [TextArea] public string FlavorText;
        // Lets on-select order stay stable regardless of Resources.LoadAll's own ordering.
        public int SortOrder;

        // Populated by VoarVR/Configure Characters from ModelAssetPath, via the ModelImporter
        // API rather than hand authoring - a direct object reference so builds never need
        // AssetDatabase.
        public GameObject RigModel;
        public string ModelAssetPath;
        public Material BodyMaterial;
        // Renderer names ending in one of these are hidden in first-person (e.g. "DragonFace").
        public string[] FirstPersonHiddenParts = { "Face", "Neck" };
        // Rest-pose glide target distance from shoulder, meters; scale with the rig's actual
        // bone lengths so the idle pose neither overreaches nor folds a longer/shorter wing.
        public float RestArmSpan = 0.56f;

        [Header("Stats (relative to the Duck baseline)")]
        [Tooltip("Overall body/wing scale, used for the stat bar and to derive wing area/drag/mass. The rig's visual mesh size is authored separately in Blender.")]
        [Range(0.3f, 3f)] public float Size = 1f;
        [Tooltip("Top cruising/dive airspeed.")]
        [Range(0f, 1f)] public float Speed = 0.5f;
        [Tooltip("Thrust per wingbeat.")]
        [Range(0f, 1f)] public float Power = 0.5f;
        [Tooltip("Roll/pitch rate and how quickly the body responds to input.")]
        [Range(0f, 1f)] public float Agility = 0.5f;
        [Tooltip("Mass for its size: heavier holds momentum longer but stalls at higher speed.")]
        [Range(0f, 1f)] public float Weight = 0.5f;

        // Bakes the abstract stat card into the point-mass simulation's tunables. A bigger Size
        // grows wing/drag area with the square and mass with the cube, so a small-Size character
        // naturally needs to flap more often to stay up - that falls out of the physics, it is
        // not itself a stat.
        public BirdFlightProfile BuildProfile()
        {
            float sizeSq = Size * Size;
            return new BirdFlightProfile
            {
                MassKg = 1.1f * Size * sizeSq * Mathf.Lerp(0.4f, 1.6f, Weight),
                WingAreaM2 = 0.28f * sizeSq,
                BodyDragAreaM2 = 0.018f * sizeSq,
                MaxStrokeSpeed = Mathf.Lerp(1.8f, 4.2f, Speed),
                StrokeForcePerSpeedSquared = Mathf.Lerp(3f, 15f, Power) * Mathf.Max(Size, 0.5f),
                InitialSpeedMps = Mathf.Lerp(5f, 11f, Speed),
                StallSpeedMps = Mathf.Lerp(2.5f, 5.5f, Weight),
                MaxRollDeg = Mathf.Lerp(35f, 65f, Agility),
                MaxPitchDeg = Mathf.Lerp(24f, 46f, Agility),
                AttitudeResponseSeconds = Mathf.Lerp(0.42f, 0.18f, Agility),
            };
        }
    }
}
