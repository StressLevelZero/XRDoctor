using Newtonsoft.Json;

namespace SLZ.XRDoctor;

public static class WindowsMRDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(WindowsMRDiagnostics));
    private const string LogTag = "WindowsMR";

    public static void CheckDirectly(out bool hasWindowsMR) {
        var runtimePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "MixedRealityRuntime.json");
        Log.Information("[{LogTag}] Checking directly for runtime at path {runtimePath}.", LogTag, runtimePath);
        do {
            try {
                if (!File.Exists(runtimePath)) {
                    Log.Warning("[{LogTag}] Runtime JSON not found at expected path \"{runtimePath}\".", LogTag,
                        runtimePath);
                    continue;
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
                    Log.Error(e, "[{LogTag}] JSON did not parse correctly at \"{runtimePath}\".", LogTag,
                        runtimePath);
                    break;
                }

                Log.Information("[{LogTag}] Found runtime at path \"{location}\".", LogTag, runtimePath);
                hasWindowsMR = true;
            } catch (Exception e) { Log.Error(e, "[{LogTag}] Error while determining install state.", LogTag); }
        } while (false);

        hasWindowsMR = false;
    }
}