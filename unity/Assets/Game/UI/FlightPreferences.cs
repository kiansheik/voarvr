using UnityEngine;

namespace VoarVR.UI
{
    // Independent of flight authority and instrument visibility. Existing advanced embodied
    // first-person remains the default; route guidance never enables cockpit text.
    public static class FlightPreferences
    {
        public const string KeyPrefix = "VoarVR.FlightPreferences.v1.";
        public static bool FirstPersonStabilized
        {
            get => Read(KeyPrefix + "FirstPersonStabilized", false);
            set => Write(KeyPrefix + "FirstPersonStabilized", value);
        }
        public static bool GuidanceEnabled
        {
            get => Read(KeyPrefix + "GuidanceEnabled", true);
            set => Write(KeyPrefix + "GuidanceEnabled", value);
        }
        public static bool AudioEnabled
        {
            get => Read(KeyPrefix + "AudioEnabled", true);
            set => Write(KeyPrefix + "AudioEnabled", value);
        }
        public static bool HapticsEnabled
        {
            get => Read(KeyPrefix + "HapticsEnabled", true);
            set => Write(KeyPrefix + "HapticsEnabled", value);
        }
        private static bool Read(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
        private static void Write(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
