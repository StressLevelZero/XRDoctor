using Microsoft.Win32;
using Newtonsoft.Json;
using SLZ.XRDoctor;

public static class VirtualDesktopDiagnostics
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(VirtualDesktopDiagnostics));
    private const string LogTag = "VirtualDesktop";

    public static void CheckDirectly(out bool hasVirtualDesktop) {
        hasVirtualDesktop = false;
        
        Log.Information("[{LogTag}] Checking directly for runtime.", LogTag);
        try {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders");
            if (key == null) { return; }

            var openXRDirectory = string.Empty;
            foreach (var valueName in key.GetValueNames()) {
                if (valueName.EndsWith(@"Virtual Desktop Streamer\OpenXR\")) {
                    openXRDirectory = valueName;
                    hasVirtualDesktop = true;
                    break;
                }
            }

            if (!hasVirtualDesktop) {
                Log.Warning("[{LogTag}] OpenXR directory not found.", LogTag);
                return;
            }
            
            if (!Directory.Exists(openXRDirectory)) {
                Log.Warning("[{LogTag}] OpenXR directory \"{OpenXRDirectory}\" not found.", LogTag, openXRDirectory);
                return;
            }
            
            {
                var runtimePath = Path.Combine(openXRDirectory, "virtualdesktop-openxr.json");
                if (!File.Exists(runtimePath)) {
                    Log.Warning("[{LogTag}] Runtime JSON not found at expected path \"{runtimePath}\".", LogTag,
                        runtimePath);
                    goto Check32;
                }

                var runtimeJsonStr = File.ReadAllText(runtimePath);
                
                if (string.IsNullOrWhiteSpace(runtimeJsonStr)) {
                    Log.Error("[{LogTag}] Runtime JSON was empty at \"{runtimePath}\".", LogTag, runtimePath);
                    goto Check32;
                }
                
                RuntimeManifest manifest;
                try {
                    manifest = JsonConvert.DeserializeObject<RuntimeManifest>(runtimeJsonStr);
                } catch (JsonException e) {
                    Log.Error(e, "[{LogTag}] JSON did not parse correctly at \"{runtimePath}\".", LogTag,
                        runtimePath);
                    goto Check32;
                }

                Log.Information("[{LogTag}] Found runtime at path \"{location}\".", LogTag, runtimePath);
            }
            Check32:
            {
                var runtimePath = Path.Combine(openXRDirectory, "virtualdesktop-openxr-32.json");
                if (!File.Exists(runtimePath)) {
                    Log.Warning("[{LogTag}] Runtime JSON not found at expected path \"{runtimePath}\".", LogTag,
                        runtimePath);
                    return;
                }
                
                var runtimeJsonStr = File.ReadAllText(runtimePath);
                
                if (string.IsNullOrWhiteSpace(runtimeJsonStr)) {
                    Log.Error("[{LogTag}] Runtime JSON was empty at \"{runtimePath}\".", LogTag, runtimePath);
                    return;
                }
                
                RuntimeManifest manifest;
                try {
                    manifest = JsonConvert.DeserializeObject<RuntimeManifest>(runtimeJsonStr);
                } catch (JsonException e) {
                    Log.Error(e, "[{LogTag}] JSON did not parse correctly at \"{runtimePath}\".", LogTag,
                        runtimePath);
                    return;
                }
                
                Log.Information("[{LogTag}] Found runtime at path \"{location}\".", LogTag, runtimePath);
            }

        } catch (Exception e) {
            Log.Error(e, "[{LogTag}] Error while determining install state.", LogTag);
        }
    }
}