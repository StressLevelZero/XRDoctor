using Microsoft.Win32;
using Newtonsoft.Json;

namespace SLZ.XRDoctor;

public static class OculusDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(OculusDiagnostics));
    private const string LogTag = "Oculus";

    public static void CheckDirectly(out bool hasOculus) {
        Log.Information("[{LogTag}] Checking directly for Oculus runtime at {OculusRegistryKey}.", LogTag,
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Oculus");
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Oculus");
        do {
            try {
                var o = key?.GetValue("InstallLocation");
                if (o is not string installLocation || string.IsNullOrWhiteSpace(installLocation)) {
                    Log.Information("[{LogTag}] Runtime not found.", LogTag);
                    break;
                }

                var runtimePath = Path.Combine(installLocation, "Support", "oculus-runtime", "oculus_openxr_64.json");

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
                hasOculus = true;
                return;
            } catch (Exception e) { Log.Error(e, "[{LogTag}]} Error while determining install state.", LogTag); }
        } while (false);

        hasOculus = false;
    }
}