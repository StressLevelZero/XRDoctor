using Microsoft.Win32;
using Newtonsoft.Json;


namespace SLZ.XRDoctor;

public static class VulkanDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(VulkanDiagnostics));
    private const string LogTag = "Vulkan";

    public static void FindImplicitLayers(out Dictionary<string, VulkanLayerManifest> layers) {
        Log.Information("[{LogTag}] Listing implicit Vulkan API layers according to {ImplicitApiLayersRegistryKey}",
            LogTag, @"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\Vulkan\ImplicitLayers");

        layers = new Dictionary<string, VulkanLayerManifest>();

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\Vulkan\ImplicitLayers");
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

            VulkanLayerManifest manifest;
            try { manifest = JsonConvert.DeserializeObject<VulkanLayerManifest>(layerJson); } catch (JsonException e) {
                Log.Error(e, "[{LogTag}] API Layer \"{apiLayer}\" is registered but did not parse correctly.",
                    LogTag, layerJsonPath);
                continue;
            }
            
            layers[layerJsonPath] = manifest;
        }

        foreach (var (path, layerManifest) in layers) {
            Log.Information("[{LogTag}] API Layer \"{apiLayer}\" {pathToManifest}", LogTag, layerManifest.Layer.Name, path);
        }
    }
}