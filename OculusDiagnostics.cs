﻿using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;

namespace SLZ.XRDoctor;

public static class OculusDiagnostics {
    private const string RuntimeName = "Oculus";

    public static void CheckDirectly(out bool hasOculus) {
        Log.Information("Checking directly for Oculus runtime at {OculusRegistryKey}.",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Oculus");
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Oculus");
        do {
            try {
                var o = key?.GetValue("InstallLocation");
                if (o is not string installLocation || string.IsNullOrWhiteSpace(installLocation)) {
                    Log.Information("[{runtime}] Runtime not found.", RuntimeName);
                    break;
                }

                var runtimePath = Path.Combine(installLocation, "Support", "oculus-runtime", "oculus_openxr_64.json");

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
                hasOculus = true;
                return;
            } catch (Exception e) { Log.Error(e, "[{runtime}]} Error while determining install state.", RuntimeName); }
        } while (false);

        hasOculus = false;
    }
}