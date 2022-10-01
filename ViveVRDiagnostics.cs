using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;

namespace SLZ.XRDoctor;

public static class ViveVRDiagnostics {
    private const string RuntimeName = "ViveVR";

    public static void CheckDirectly(out bool hasViveVR) {
        Log.Information("Checking directly for Vive OpenXR runtime at {ViveVRRegistryKey}.",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\HtcVive\Updater");
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\HtcVive\Updater");
        do {
            try {
                var o = key?.GetValue("AppPath");

                if (o is not string viveLocation || string.IsNullOrWhiteSpace(viveLocation)) {
                    Log.Information("[{runtime}] Vive Updater not found.", RuntimeName);
                    break;
                }

                var runtimePath = Path.Combine(viveLocation, "App", "ViveVRRuntime", "ViveVR_openxr",
                    "ViveOpenXR.json");

                if (!File.Exists(runtimePath)) {
                    Log.Warning("[{runtime}] Runtime JSON not found at expected path \"{runtimePath}\".", RuntimeName,
                        runtimePath);
                    break;
                }

                var runtimeJsonStr = File.ReadAllText(runtimePath);

                if (string.IsNullOrWhiteSpace(runtimeJsonStr)) {
                    Log.Error("[{runtime}] Runtime JSON was empty at \"{runtimePath}\".", RuntimeName, runtimePath);
                    break;
                }

                RuntimeManifest manifest;
                try {
                    manifest = JsonConvert.DeserializeObject<RuntimeManifest>(runtimeJsonStr);
                } catch (JsonException e) {
                    Log.Error(e, "[{runtime}] Runtime JSON did not parse correctly at {runtimePath}.", RuntimeName,
                        runtimePath);
                    break;
                }

                Log.Information("[{runtime}] Runtime found at path \"{location}\".", RuntimeName, runtimePath);
                hasViveVR = true;
                return;
            } catch (Exception e) { Log.Error(e, "[{runtime}]} Error while determining install state.", RuntimeName); }
        } while (false);

        hasViveVR = false;
    }
}