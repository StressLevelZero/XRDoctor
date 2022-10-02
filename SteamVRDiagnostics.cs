using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Valve.VR;

namespace SLZ.XRDoctor;

public static class SteamVRDiagnostics {
    private const string LogTag = "SteamVR";

    public static void CheckDirectly(out bool hasSteamVR) {
        Log.Information("[{LogTag}] Checking directly for runtime at {SteamRegistryKey}.", LogTag,
            @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam");
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
        do {
            try {
                var o = key?.GetValue("SteamPath");
                if (o is not string steamLocation || string.IsNullOrWhiteSpace(steamLocation)) {
                    Log.Information("[{LogTag}] Not found.", LogTag);
                    break;
                }

                var lfPath = Path.Combine(steamLocation, "steamapps", "libraryfolders.vdf");

                if (!File.Exists(lfPath)) {
                    Log.Warning("[{LogTag}] Could not find libraryfolders.vdf at \"{lfPath}\"", LogTag, lfPath);
                    break;
                }

                var folderCount = 0;

                var lfStr = File.ReadAllText(lfPath);
                var lf = VdfConvert.Deserialize(lfStr);
                var lfJson = lf.ToJson().Value.Values();
                foreach (var folderDict in lfJson) {
                    if (folderDict is not JObject folder) { continue; }

                    if (!folder.TryGetValue("path", out var pathJToken)) { continue; }

                    var path = pathJToken.ToObject<string>();
                    if (string.IsNullOrWhiteSpace(path)) { continue; }

                    if (!Directory.Exists(path)) { continue; }

                    var runtimePath = Path.Combine(path, "steamapps", "common", "SteamVR", "steamxr_win64.json");

                    Log.Warning(
                        "[{LogTag}] Checking Steam install directory for Runtime JSON at path: \"{runtimePath}\".",
                        LogTag,
                        runtimePath);

                    if (!File.Exists(runtimePath)) {
                        Log.Warning("[{LogTag}] Runtime JSON not found at expected path \"{runtimePath}\".",
                            LogTag,
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
                    hasSteamVR = true;
                }
            } catch (Exception e) { Log.Error(e, "[{LogTag}]} Error while determining install state.", LogTag); }
        } while (false);

        hasSteamVR = false;
    }

    public static void CheckDevices() {
        var exePath = AppContext.BaseDirectory;
        var appManifest = Path.Combine(exePath, "app.vrmanifest");
        var actionsJson = Path.Combine(exePath, "actions.json");

        var err = default(EVRInitError);
        var sys = OpenVR.Init(ref err, EVRApplicationType.VRApplication_Overlay);
        if (err != EVRInitError.None) { Log.Error("[{LogTag}] Error initting {err}", LogTag, err); }

        var appManifestResult = OpenVR.Applications.AddApplicationManifest(appManifest, false);
        if (appManifestResult != EVRApplicationError.None) {
            Log.Error("[{LogTag}] Error adding app manifest: {appManifestResult}", LogTag, appManifestResult);
        }

        var actionManifestResult = OpenVR.Input.SetActionManifestPath(actionsJson);
        if (actionManifestResult != EVRInputError.None) {
            Log.Error("[{LogTag}] Error setting action manifest: {actionManifestResult}", LogTag,
                actionManifestResult);
        }

        ulong defaultActionSetHandle = 0;
        var ashResult = OpenVR.Input.GetActionSetHandle("/actions/default", ref defaultActionSetHandle);
        if (ashResult != EVRInputError.None) {
            Log.Error("[{LogTag}] Error getting action set handle for default action set: {ashResult}", LogTag,
                ashResult);
        }

        VRActiveActionSet_t lhaas = default;
        lhaas.ulActionSet = defaultActionSetHandle;
        var aas = new[] {lhaas};

        ETrackedPropertyError ipdError = default;
        var ipd = OpenVR.System.GetFloatTrackedDeviceProperty(0, ETrackedDeviceProperty.Prop_UserIpdMeters_Float,
            ref ipdError);
        if (ipdError == ETrackedPropertyError.TrackedProp_Success) {
            Log.Information("[{LogTag}] IPD: {ipd}", LogTag, ipd);
        } else { Log.Error("[{LogTag}] Error updating IPD: {ipdError}", LogTag, ipdError); }

        var left = -1;
        var right = -1;

        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) {
            var trackedDeviceClass = OpenVR.System.GetTrackedDeviceClass(i);

            if (trackedDeviceClass != ETrackedDeviceClass.Invalid) {
                var propErr = ETrackedPropertyError.TrackedProp_Success;
                var count = OpenVR.System.GetStringTrackedDeviceProperty(i,
                    ETrackedDeviceProperty.Prop_ModelNumber_String, null, 0, ref propErr);
                var modelNumber = new System.Text.StringBuilder((int) count);
                OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ModelNumber_String,
                    modelNumber, count, ref propErr);

                Log.Information("[{LogTag}] Device {i}: modelNumber={modelNumber} class={class}", LogTag, i,
                    modelNumber, trackedDeviceClass);
            }

            if (trackedDeviceClass == ETrackedDeviceClass.Controller) {
                var role = OpenVR.System.GetControllerRoleForTrackedDeviceIndex(i);
                switch (role) {
                    case ETrackedControllerRole.LeftHand:
                        left = (int) i;
                        break;
                    case ETrackedControllerRole.RightHand:
                        right = (int) i;
                        break;
                }
            }
        }

        OpenVR.Shutdown();
    }
}