using Newtonsoft.Json;
using Serilog;
using SLZ.XRDoctor;

public static class WindowsMRDiagnostics {
    private const string RuntimeName = "WindowsMR";

    public static void CheckDirectly(out bool hasWindowsMR) {
        var runtimePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "MixedRealityRuntime.json");
        Log.Information("[{runtime}] Checking directly for runtime at path {runtimePath}.", RuntimeName, runtimePath);
        do {
            try {
                if (!File.Exists(runtimePath)) {
                    Log.Warning("[{runtime}] Runtime JSON not found at expected path \"{runtimePath}\".", RuntimeName,
                        runtimePath);
                    continue;
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
                    Log.Error(e, "[{runtime}] JSON did not parse correctly at \"{runtimePath}\".", RuntimeName,
                        runtimePath);
                    break;
                }

                Log.Information("[{runtime}] Found runtime at path \"{location}\".", RuntimeName, runtimePath);
                hasWindowsMR = true;
            } catch (Exception e) { Log.Error(e, "[{runtime}]} Error while determining install state.", RuntimeName); }
        } while (false);

        hasWindowsMR = false;
    }
}