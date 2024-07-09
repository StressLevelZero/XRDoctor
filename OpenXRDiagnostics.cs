using Microsoft.Win32;
using Newtonsoft.Json;

namespace SLZ.XRDoctor;

public static class OpenXRDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(OpenXRDiagnostics));
    private const string LogTag = "OpenXR";

    public static void FindActiveRuntime(out string XR_RUNTIME_JSON) {
        Log.Information("[{LogTag}] Finding active runtime according to {ActiveRuntimeRegistryKey}",
            LogTag, @"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1");

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
        Log.Information("[{LogTag}] Listing available runtimes according to {AvailableRuntimesRegistryKey}",
            LogTag, @"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1\AvailableRuntimes");

        runtimes = new Dictionary<string, RuntimeManifest>();

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\OpenXR\1\AvailableRuntimes");
        if (key == null) { return; }

        foreach (var runtimeName in key.GetValueNames()) {
            if (key.GetValue(runtimeName) is not (int and 0)) { continue; }

            if (!File.Exists(runtimeName)) {
                Log.Error("[{LogTag}] Runtime \"{apiLayer}\" is registered but was not found.", LogTag, runtimeName);
                continue;
            }

            var runtimeJsonStr = File.ReadAllText(runtimeName);
            if (string.IsNullOrWhiteSpace(runtimeJsonStr)) {
                Log.Error("[{LogTag}] Runtime \"{apiLayer}\" is registered but its JSON was empty.", LogTag, runtimeName);
                continue;
            }

            RuntimeManifest manifest;
            try { manifest = JsonConvert.DeserializeObject<RuntimeManifest>(runtimeJsonStr); } catch (JsonException e) {
                Log.Error(e, "[{LogTag}] Runtime \"{apiLayer}\" is registered but its JSON did not parse correctly.",
                    LogTag, runtimeName);
                continue;
            }

            runtimes[runtimeName] = manifest;
        }
    }
    
    public static void FindImplicitApiLayers(out Dictionary<string, ApiLayerManifest> layers) {
        Log.Information("[{LogTag}] Listing implicit OpenXR API layers according to {ImplicitApiLayersRegistryKey}",
            LogTag, @"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit");

        layers = new Dictionary<string, ApiLayerManifest>();

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit");
        if (key == null) {
            Log.Information("[{LogTag}] No implicit API layers (This isn't unusual).", LogTag);
            return;
        }

        foreach (var layerJsonPath in key.GetValueNames()) {
            if (key.GetValue(layerJsonPath) is not (int and 0)) { continue; }

            if (!File.Exists(layerJsonPath)) {
                Log.Error("[{LogTag}] API Layer \"{apiLayer}\" is registered but was not found.", LogTag, layerJsonPath);
                continue;
            }

            var layerJson = File.ReadAllText(layerJsonPath);
            if (string.IsNullOrWhiteSpace(layerJson)) {
                Log.Error("[{LogTag}] API Layer \"{apiLayer}\" is registered but was empty.", LogTag, layerJsonPath);
                continue;
            }

            ApiLayerManifest manifest;
            try { manifest = JsonConvert.DeserializeObject<ApiLayerManifest>(layerJson); } catch (JsonException e) {
                Log.Error(e, "[{LogTag}] API Layer \"{apiLayer}\" is registered but did not parse correctly.",
                    LogTag, layerJsonPath);
                continue;
            }
            
            layers[layerJsonPath] = manifest;
        }

        foreach (var (path, layerManifest) in layers) {
            var isDisabled = false;
            var disableVar = layerManifest.ApiLayer.DisableEnvironment;
            if (!string.IsNullOrWhiteSpace(disableVar)) {
                isDisabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(disableVar));
            }

            var isEnabled = false;
            var enableVar = layerManifest.ApiLayer.EnableEnvironment;
            if (!string.IsNullOrWhiteSpace(enableVar)) {
                isEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(enableVar));
            }

            bool actuallyEnabled;

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            // Overly pedantic for avoidance of confusion
            if (isDisabled && isEnabled) {
                actuallyEnabled = false;
            } else if (isDisabled) {
                actuallyEnabled = false;
            } else if (isEnabled) {
                actuallyEnabled = true;
            } else {
                actuallyEnabled = true;
            }
            
            Log.Information("[{LogTag}] API Layer \"{apiLayer}\" (Enabled: {enabled} DisableEnvironment = {disableEnvironment}, EnableEnvironment = {enableEnvironment}) {pathToManifest}", LogTag, layerManifest.ApiLayer.Name, actuallyEnabled, isDisabled, isEnabled, path);
        }
    }
}