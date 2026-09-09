using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace VoarVR.Editor
{
    public static class ProjectSetup
    {
        public static readonly string[] Scenes =
        {
            "Assets/Scenes/Bootstrap/Bootstrap.unity",
            "Assets/Scenes/Menu/CharacterSelect.unity",
            "Assets/Scenes/Prototypes/BirdFlight.unity"
        };

        // Explicitly invoked; never runs on import or replaces authored scenes.
        [MenuItem("VoarVR/Configure Foundation")]
        public static void Configure()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "VoarVR";
            PlayerSettings.productName = "VoarVR";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.voarvr.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            // Unity exposes active input handling through serialized PlayerSettings.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handling = settings.FindProperty("activeInputHandler");
            if (handling == null) throw new InvalidOperationException("Unity activeInputHandler setting is unavailable.");
            handling.intValue = 1; // Input System only; editor restart may be required.
            settings.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.Refresh();
            const string pipelinePath = "Assets/Settings/VoarVRPipeline.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                const string rendererPath = "Assets/Settings/VoarVRRenderer.asset";
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, rendererPath);
                }
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 4;
                pipeline.renderScale = 1f;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int quality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(quality, false);
            bool xrConfigured = ConfigureXR();
            EditorBuildSettings.scenes = Array.ConvertAll(Scenes, path => new EditorBuildSettingsScene(path, true));
            AssetDatabase.SaveAssets();
            Debug.Log(xrConfigured
                ? "Foundation configured, including Android OpenXR. Run tests and Android XR Project Validation."
                : "Desktop foundation configured. Android OpenXR features remain unconfigured until Android Build Support is installed.");
        }

        private static bool ConfigureXR()
        {
            // OpenXR 1.18 intentionally returns null settings for unsupported targets.
            // Keep desktop setup usable while reporting the missing Hub module clearly.
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning($"Install Android Build Support (SDK & NDK Tools and OpenJDK) for Unity {Application.unityVersion} in Unity Hub, then rerun Configure Foundation. Android XR configuration is deferred.");
                return false;
            }
            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.settingsKey, out var perTarget))
            {
                const string path = "Assets/Settings/XRGeneralSettings.asset";
                perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (perTarget == null)
                {
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(perTarget, path);
                }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }
            const BuildTargetGroup target = BuildTargetGroup.Android;
            if (!perTarget.HasSettingsForBuildTarget(target)) perTarget.CreateDefaultSettingsForBuildTarget(target);
            if (!perTarget.HasManagerSettingsForBuildTarget(target)) perTarget.CreateDefaultManagerSettingsForBuildTarget(target);
            var general = perTarget.SettingsForBuildTarget(target);
            general.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", target))
                throw new InvalidOperationException("Could not assign Android OpenXR loader.");
            FeatureHelpers.RefreshFeatures(target);
            var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (xr == null) throw new InvalidOperationException("OpenXR settings were not created.");
            var quest = xr.GetFeature<MetaQuestFeature>();
            var touch = xr.GetFeature<OculusTouchControllerProfile>();
            var touchPlus = xr.GetFeature<MetaQuestTouchPlusControllerProfile>();
            if (quest == null || touch == null || touchPlus == null)
                throw new InvalidOperationException("Required Quest/Touch/Touch Plus features missing.");
            quest.enabled = true;
            touch.enabled = true; // Kept for older Quest 1/2 Touch controllers.
            touchPlus.enabled = true; // Quest 3's actual controllers (and Quest 3S).
            xr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            xr.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
            EditorUtility.SetDirty(quest);
            EditorUtility.SetDirty(touch);
            EditorUtility.SetDirty(touchPlus);
            EditorUtility.SetDirty(xr);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);
            EditorUtility.SetDirty(perTarget);
            return true;
        }

        [MenuItem("VoarVR/Build Quest Development APK")]
        public static void BuildQuest()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Switch to Android in Build Profiles first (CLI: -buildTarget Android).");
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                throw new BuildFailedException("Run VoarVR/Configure Foundation first.");
            var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (general == null || !general.InitManagerOnStart || general.Manager == null
                || !System.Linq.Enumerable.Any(general.Manager.activeLoaders, loader => loader is OpenXRLoader)
                || xr == null || xr.GetFeature<MetaQuestFeature>()?.enabled != true
                || xr.GetFeature<OculusTouchControllerProfile>()?.enabled != true
                || xr.GetFeature<MetaQuestTouchPlusControllerProfile>()?.enabled != true)
                throw new BuildFailedException("Android OpenXR/Quest setup is incomplete (Touch and/or Touch Plus profile disabled). Run Configure Foundation.");
            ValidateCharacterCatalog();
            Directory.CreateDirectory("../builds/quest");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "../builds/quest/VoarVR.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Quest build failed: " + report.summary.result);
        }

        // A successful BuildPlayer() call only proves an APK was written, not that it will do
        // anything once launched - this is what actually caught the CharacterSelect regression
        // (a broken xrRigPrefab reference or a character missing its RigModel builds fine and
        // then renders a blank menu on device). Checked before every Quest build.
        private static void ValidateCharacterCatalog()
        {
            foreach (var scenePath in Scenes)
                if (!System.IO.File.Exists(scenePath))
                    throw new BuildFailedException("Missing scene: " + scenePath);

            var selectScene = System.IO.File.ReadAllText("Assets/Scenes/Menu/CharacterSelect.unity");
            if (!System.Text.RegularExpressions.Regex.IsMatch(selectScene, @"xrRigPrefab: \{fileID: -?\d+, guid: [0-9a-f]{32}"))
                throw new BuildFailedException("CharacterSelect's CharacterSelectController has no xrRigPrefab assigned.");

            var guids = AssetDatabase.FindAssets("t:" + nameof(VoarVR.Flight.BirdCharacterDefinition));
            if (guids.Length == 0) throw new BuildFailedException("No BirdCharacterDefinition assets exist under Resources/Characters.");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var character = AssetDatabase.LoadAssetAtPath<VoarVR.Flight.BirdCharacterDefinition>(path);
                if (character.RigModel == null)
                    throw new BuildFailedException(character.DisplayName + " (" + path + ") has no RigModel. Run VoarVR/Configure Characters.");
                if (character.BodyMaterial == null)
                    throw new BuildFailedException(character.DisplayName + " (" + path + ") has no BodyMaterial.");
                var bones = character.RigModel.GetComponentsInChildren<Transform>();
                foreach (var required in new[] { "LeftUpper", "LeftForearm", "LeftHand", "LeftTip", "RightUpper", "RightForearm", "RightHand", "RightTip" })
                    if (!System.Linq.Enumerable.Any(bones, t => t.name == required))
                        throw new BuildFailedException(character.DisplayName + " RigModel is missing bone \"" + required + "\".");
                var renderers = character.RigModel.GetComponentsInChildren<Renderer>();
                if (!System.Linq.Enumerable.Any(renderers, r => r.name.EndsWith("Face")))
                    throw new BuildFailedException(character.DisplayName + " RigModel has no renderer ending in \"Face\".");
            }
        }
    }
}
