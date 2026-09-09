using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace VoarVR.Editor
{
    // Explicit recovery after an editor restart; no import-time actions or runtime dependency.
    public static class McpSessionRecovery
    {
        [MenuItem("VoarVR/Tools/Reconnect Installed MCP Session")]
        public static async void Reconnect()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                    a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator") != null);
                if (assembly == null) throw new InvalidOperationException("MCP for Unity is not installed.");
                var locator = assembly.GetType("MCPForUnity.Editor.Services.MCPServiceLocator");
                var manager = locator.GetProperty("TransportManager").GetValue(null);
                var start = manager.GetType().GetMethod("StartAsync");
                var mode = Enum.Parse(start.GetParameters()[0].ParameterType, "Http");
                var task = (Task<bool>)start.Invoke(manager, new[] { mode });
                Debug.Log(await task ? "MCP session reconnected." : "MCP session reconnect failed; check the local server.");
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
