using Meta.XR;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace VoarVR.Editor
{
    /// <summary>Explicit configuration for the controller-free Quest build.</summary>
    public static class HandFlightSetup
    {
        [MenuItem("VoarVR/Configure Hand Flight")]
        public static void Configure()
        {
            const BuildTargetGroup target = BuildTargetGroup.Android;
            FeatureHelpers.RefreshFeatures(target);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            var meta = settings != null ? settings.GetFeature<MetaXRFeature>() : null;
            if (meta == null)
                throw new BuildFailedException("Meta XR Core SDK or Android OpenXR settings are missing. Resolve packages and Configure Foundation first.");
            var project = OVRProjectConfig.CachedProjectConfig;
            var runtime = OVRRuntimeSettings.Instance;
            if (project == null || runtime == null)
                throw new BuildFailedException("Meta XR settings are not ready. Wait for package import to complete, then configure hand flight again.");

            // Core SDK's feature hooks the existing Unity OpenXR session. No additional rig,
            // camera or Oculus XR loader is needed for the adapter's raw hand-state API.
            meta.enabled = true;
            EditorUtility.SetDirty(meta);
            EditorUtility.SetDirty(settings);

            project.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            project.handTrackingFrequency = OVRProjectConfig.HandTrackingFrequency.LOW;
            project.bodyTrackingSupport = OVRProjectConfig.FeatureSupport.Required;
            OVRProjectConfig.CommitProjectConfig(project);

            runtime.HandSkeletonVersion = OVRHandSkeletonVersion.OpenXR;
            OVRRuntimeSettings.CommitRuntimeSettings(runtime);
            AssetDatabase.SaveAssets();
            ValidateAndroidBuild();
            Debug.Log("Quest hand flight configured: hands required, OpenXR skeleton, Meta XR feature and body-tracking permission. Fast Motion Mode remains at the default frequency pending headset comparison.");
        }

        public static void ValidateAndroidBuild()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null || settings.GetFeature<MetaXRFeature>()?.enabled != true)
                throw new BuildFailedException("Hand flight requires Meta XR Feature on Android OpenXR. Run VoarVR/Configure Hand Flight.");

            var project = OVRProjectConfig.CachedProjectConfig;
            if (project == null)
                throw new BuildFailedException("Meta XR project settings are unavailable. Resolve packages and run VoarVR/Configure Hand Flight.");
            if (project.handTrackingSupport != OVRProjectConfig.HandTrackingSupport.HandsOnly)
                throw new BuildFailedException("This branch must launch without controllers. Set Hands Only with VoarVR/Configure Hand Flight before building.");
            if (project.bodyTrackingSupport != OVRProjectConfig.FeatureSupport.Required)
                throw new BuildFailedException("Wide Motion Mode requires body-tracking support. Run VoarVR/Configure Hand Flight.");
            var runtime = OVRRuntimeSettings.Instance;
            if (runtime == null || runtime.HandSkeletonVersion != OVRHandSkeletonVersion.OpenXR)
                throw new BuildFailedException("Hand flight requires the OpenXR hand skeleton. Run VoarVR/Configure Hand Flight.");
        }
    }

    // Validate all Android build entry points, including Build Profiles and command-line builds.
    // This hook checks configuration only; it never mutates settings during a build or import.
    public sealed class HandFlightBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
                HandFlightSetup.ValidateAndroidBuild();
        }
    }
}
