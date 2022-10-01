using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace SLZ.XRDoctor; 

public static class OpenXRDiagnostics {
    public static void FindActiveRuntime(out string XR_RUNTIME_JSON) {
        Log.Information("Finding active runtime according to {ActiveRuntimeRegistryKey}",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1");

        // I.e.
        // C:\Program Files (x86)\Steam\steamapps\common\SteamVR\steamxr_win64.json
        // or
        // C:\Program Files\Oculus\Support\oculus-runtime\oculus_openxr_64.json
        XR_RUNTIME_JSON = null;

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\OpenXR\1");
        var o = key?.GetValue("ActiveRuntime");
        if (o is string activeRuntimeStr && !string.IsNullOrWhiteSpace(activeRuntimeStr)) {
            XR_RUNTIME_JSON = activeRuntimeStr;
        }
    }

    public static void FindRegisteredRuntimes(out Dictionary<string, RuntimeManifest> runtimes) {
        Log.Information("Listing available runtimes according to {AvailableRuntimesRegistryKey}",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1\AvailableRuntimes");

        runtimes = new Dictionary<string, RuntimeManifest>();

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\OpenXR\1\AvailableRuntimes");
        if (key == null) { return; }

        foreach (var runtimeName in key.GetValueNames()) {
            if (key.GetValue(runtimeName) is not (int and 0)) { continue; }

            if (!File.Exists(runtimeName)) {
                Log.Error("Runtime \"{runtimeName}\" is registered but was not found.", runtimeName);
                continue;
            }

            var runtimeJsonStr = File.ReadAllText(runtimeName);
            if (string.IsNullOrWhiteSpace(runtimeJsonStr)) {
                Log.Error("Runtime \"{runtimeName}\" is registered but its JSON was empty.", runtimeName);
                continue;
            }

            RuntimeManifest manifest;
            try { manifest = JsonConvert.DeserializeObject<RuntimeManifest>(runtimeJsonStr); } catch (JsonException e) {
                Log.Error(e, "Runtime \"{runtimeName}\" is registered but its JSON did not parse correctly.",
                    runtimeName);
                continue;
            }

            runtimes[runtimeName] = manifest;
        }
    }
}