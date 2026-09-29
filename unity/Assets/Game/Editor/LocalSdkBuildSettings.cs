using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VoarVR.Editor
{
    // Core 207's DevAgentBuildProcessor injects the local debugger token/address
    // even with the assistant disabled. Strip those unused values after its order-1
    // callback, before Unity serializes Resources into the player.
    public sealed class LocalSdkBuildSettings : IPreprocessBuildWithReport
    {
        public const string AssetPath = "Assets/Resources/DevAgentSettings.asset";
        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report) => Sanitize();

        [MenuItem("VoarVR/Tools/Clear Local SDK Settings From Player")]
        public static void Sanitize()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetPath);
            if (asset == null) return;
            var settings = new SerializedObject(asset);
            var enabled = Required(settings, "enabled", SerializedPropertyType.Boolean);
            var address = Required(settings, "serverAddress", SerializedPropertyType.String);
            var token = Required(settings, "accessToken", SerializedPropertyType.String);
            var voiceToken = Required(settings, "witClientAccessToken", SerializedPropertyType.String);
            enabled.boolValue = false;
            address.stringValue = token.stringValue = voiceToken.stringValue = string.Empty;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        // Explicit post-build check. Read the SDK's local editor preferences directly;
        // never print or persist the token, and never return its contents to callers.
        public static string VerifyApk(string path = "../builds/quest/VoarVR.apk")
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var settingsType = SdkType("Meta.XR.AI.AgentBridge.RemoteAgentSettings");
            var setting = settingsType.GetField("AccessToken", flags)?.GetValue(null);
            var token = setting?.GetType().GetProperty("Value")?.GetValue(setting) as string;
            var networkType = SdkType("Meta.XR.AI.AgentBridge.NetworkUtilities");
            var address = networkType.GetMethod("GetLocalNetworkAddress", flags)?.Invoke(null, null) as string;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(address))
                throw new BuildFailedException("Cannot verify local SDK values against the APK: the SDK lookup contract changed or values are unavailable.");

            var patterns = new[] { token, address }.SelectMany(value => new[]
                { Encoding.UTF8.GetBytes(value), Encoding.Unicode.GetBytes(value), Encoding.BigEndianUnicode.GetBytes(value) }).ToArray();
            bool playerConnection = false;
            using (var archive = ZipFile.OpenRead(path))
            {
                foreach (var entry in archive.Entries)
                {
                    bool found;
                    if (entry.FullName == BootConfig)
                    {
                        // Unity writes the editor address for Development-build profiler
                        // connections. Exempt only that exact line; scan the rest as usual.
                        string text;
                        using (var reader = new StreamReader(entry.Open())) text = reader.ReadToEnd();
                        var lines = text.Split('\n');
                        playerConnection = lines.Any(line => line.TrimEnd('\r') == PlayerConnectionIp + address);
                        var remainder = string.Join("\n", lines.Where(line => line.TrimEnd('\r') != PlayerConnectionIp + address));
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(remainder))) found = ContainsAny(stream, patterns);
                    }
                    else using (var stream = entry.Open()) found = ContainsAny(stream, patterns);
                    if (found)
                        throw new BuildFailedException("Local SDK connection data found in APK entry " + entry.FullName + ". Do not install or distribute this APK.");
                }
            }
            return "PASS: local SDK access token and LAN fallback are absent from all decompressed APK entries (UTF-8 and UTF-16)."
                + (playerConnection ? " Unity's Development-build profiler address (boot.config player-connection-ip) is present; release builds omit it." : "");
        }

        private const string BootConfig = "assets/bin/Data/boot.config";
        private const string PlayerConnectionIp = "player-connection-ip=";

        private static Type SdkType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).FirstOrDefault(candidate => candidate != null);
            return type ?? throw new BuildFailedException("Cannot verify SDK settings: expected SDK type is unavailable.");
        }

        private static bool ContainsAny(Stream stream, byte[][] patterns)
        {
            int overlap = patterns.Max(pattern => pattern.Length) - 1;
            var buffer = new byte[65536 + overlap];
            int retained = 0, read;
            while ((read = stream.Read(buffer, retained, buffer.Length - retained)) > 0)
            {
                int count = retained + read;
                foreach (var pattern in patterns)
                    for (int start = 0; start <= count - pattern.Length; start++)
                    {
                        if (buffer[start] != pattern[0]) continue;
                        int matched = 1;
                        while (matched < pattern.Length && buffer[start + matched] == pattern[matched]) matched++;
                        if (matched == pattern.Length) return true;
                    }
                retained = Math.Min(overlap, count);
                Array.Copy(buffer, count - retained, buffer, 0, retained);
            }
            return false;
        }

        private static SerializedProperty Required(SerializedObject settings, string name,
            SerializedPropertyType type)
        {
            var property = settings.FindProperty(name);
            if (property == null || property.propertyType != type)
                throw new BuildFailedException("Meta SDK debugger settings changed. Review LocalSdkBuildSettings before building; local credentials must remain outside the player.");
            return property;
        }
    }
}
