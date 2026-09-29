using System;

namespace VoarVR.Input
{
    public interface ITrackedFlightInput : IFlightInput, IDisposable
    {
        FlightInputFrame LastDeviceFrame { get; }
        FlightInputFrame LastRawFrame { get; }
        bool LastWingsEnabled { get; }
        bool WingsEnabled { get; set; }
        bool ResetEnabled { get; set; }
        void ResetDerivatives();
        void ResetTrackingOrigin();
    }

    public static class HandInputSettings
    {
#if UNITY_EDITOR
        public static bool? EditorOverride;
#endif
        public static bool UseHands
        {
            get
            {
#if UNITY_EDITOR
                return EditorOverride ?? false;
#else
                return UnityEngine.Application.platform == UnityEngine.RuntimePlatform.Android;
#endif
            }
        }
    }
}
