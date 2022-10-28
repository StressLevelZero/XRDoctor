using Microsoft.Win32;
using Newtonsoft.Json;

namespace SLZ.XRDoctor;

public static class ViveVRDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(ViveVRDiagnostics));
    private const string LogTag = "ViveVR";

    public static void CheckDirectly(out bool hasViveVR) {
        Log.Information("[{LogTag}] Checking directly for Vive OpenXR runtime at {ViveVRRegistryKey}.",
            LogTag, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\HtcVive\Updater");
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\HtcVive\Updater");
        do {
            try {
                var o = key?.GetValue("AppPath");

                if (o is not string viveLocation || string.IsNullOrWhiteSpace(viveLocation)) {
                    Log.Information("[{LogTag}] Vive Updater not found.", LogTag);
                    break;
                }

                var runtimePath = Path.Combine(viveLocation, "App", "ViveVRRuntime", "ViveVR_openxr",
                    "ViveOpenXR.json");

                if (!File.Exists(runtimePath)) {
                    Log.Warning("[{LogTag}] Runtime JSON not found at expected path \"{runtimePath}\".", LogTag,
                        runtimePath);
                    break;
                }

                var runtimeJsonStr = File.ReadAllText(runtimePath);

                if (string.IsNullOrWhiteSpace(runtimeJsonStr)) {
                    Log.Error("[{LogTag}] Runtime JSON was empty at \"{runtimePath}\".", LogTag, runtimePath);
                    break;
                }

                RuntimeManifest manifest;
                try {
                    manifest = JsonConvert.DeserializeObject<RuntimeManifest>(runtimeJsonStr);
                } catch (JsonException e) {
                    Log.Error(e, "[{LogTag}] Runtime JSON did not parse correctly at {runtimePath}.", LogTag,
                        runtimePath);
                    break;
                }

                Log.Information("[{LogTag}] Runtime found at path \"{location}\".", LogTag, runtimePath);
                hasViveVR = true;
                return;
            } catch (Exception e) { Log.Error(e, "[{LogTag}]} Error while determining install state.", LogTag); }
        } while (false);

        hasViveVR = false;
    }
}