using System.Globalization;
using System.Runtime.InteropServices;
using Figgle;
using Serilog;
using Silk.NET.OpenXR;
using SLZ.XRDoctor;

var programName = FiggleFonts.Standard.Render("SLZ OpenXR Doctor");
Console.WriteLine(programName);

const string outputTemplate = "[{Timestamp:HH:mm:ss} {Level}] {Message:l}{NewLine}{Exception}";

var utcNowStr = DateTime.UtcNow
    .ToString(CultureInfo.InvariantCulture.DateTimeFormat.UniversalSortableDateTimePattern,CultureInfo.InvariantCulture);
var localNowStr = DateTime.UtcNow.ToLocalTime()
    .ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern, CultureInfo.InvariantCulture);

var sanitizedNowStr = new string(localNowStr
    .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)
    .ToArray());
var logFilename = $"xrdoctor-{sanitizedNowStr}.txt";


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.File(logFilename, outputTemplate: outputTemplate)
    .MinimumLevel.Debug()
    .CreateLogger();

Log.Information("[{LogTag}] Starting log at {UTC} (UTC) {local} (Local)", "XRDoctor", utcNowStr, localNowStr);

OpenXRDiagnostics.FindActiveRuntime(out var XR_RUNTIME_JSON);
if (!string.IsNullOrWhiteSpace(XR_RUNTIME_JSON)) {
    Log.Information("[{LogTag}] Active Runtime (XR_RUNTIME_JSON): {XR_RUNTIME_JSON}", "OpenXR", XR_RUNTIME_JSON);
} else { Log.Error("[{LogTag}] Could not find an active OpenXR runtime.", "OpenXR"); }

OpenXRDiagnostics.FindRegisteredRuntimes(out var runtimes);
foreach (var (runtimeName, manifest) in runtimes) {
    if (string.IsNullOrWhiteSpace(manifest.Runtime.Name)) {
        Log.Information("[{LogTag}] Available Runtime: {runtime} - {@manifest}", "OpenXR", runtimeName, manifest);
    } else {
        Log.Information("[{LogTag}] Available Runtime: \"{name}\" - {runtime} - {@manifest}", "OpenXR", manifest.Runtime.Name,
            runtimeName,
            manifest);
    }
}

OpenXRDiagnostics.FindImplicitApiLayers(out var implicitLayers);

OculusDiagnostics.CheckDirectly(out var hasOculus);
SteamVRDiagnostics.CheckDirectly(out var hasSteamVR);
ViveVRDiagnostics.CheckDirectly(out var hasViveVr);
WindowsMRDiagnostics.CheckDirectly(out var hasWindowsMR);

HardwareDiagnostics.FindHeadsets(out var headsets);

if (XR_RUNTIME_JSON.Contains("Steam", StringComparison.InvariantCultureIgnoreCase)) {
    SteamVRDiagnostics.CheckDevices();
}

SystemDiagnostics.Check();
GameDiagnostics.Check();

#region OpenXR

Log.Information("[{LogTag}] Loading OpenXR.", "OpenXR");
var xr = XR.GetApi();

unsafe {
    uint count = 0;
    xr.EnumerateApiLayerProperties(count, &count, null);
    Span<ApiLayerProperties> layers = stackalloc ApiLayerProperties[(int) count];
    for (var i = 0; i < count; i++) { layers[i] = new ApiLayerProperties(StructureType.ApiLayerProperties); }

    xr.EnumerateApiLayerProperties(ref count, layers);

    IDictionary<string, uint> supportedLayers = new Dictionary<string, uint>();

    foreach (var layer in layers) {
        string name;
        unsafe { name = Marshal.PtrToStringUTF8((IntPtr) layer.LayerName); }

        supportedLayers[name] = layer.LayerVersion;
        Log.Information("[{LogTag}] API Layer: Name={Name} Version={Version}", "OpenXR", name, layer.LayerVersion);
    }

    ISet<string> supportedExtensions = new HashSet<string>();

    uint instanceExtensionCount = 0;
    xr.EnumerateInstanceExtensionProperties((byte*) IntPtr.Zero, instanceExtensionCount, &instanceExtensionCount, null);
    Span<ExtensionProperties> exts = stackalloc ExtensionProperties[(int) instanceExtensionCount];
    for (var i = 0; i < instanceExtensionCount; i++) {
        exts[i] = new ExtensionProperties(StructureType.TypeExtensionProperties);
    }

    xr.EnumerateInstanceExtensionProperties((string) null, ref instanceExtensionCount, exts);

    foreach (var extensionProp in exts) {
        string name;
        unsafe { name = Marshal.PtrToStringUTF8((IntPtr) extensionProp.ExtensionName); }

        supportedExtensions.Add(name);
        Log.Information("[{LogTag}][Instance] Extension: Name={Name} Version={Version}", "OpenXR", name, extensionProp.ExtensionVersion);
    }

    var ici = new InstanceCreateInfo(StructureType.InstanceCreateInfo) {
        EnabledApiLayerCount = 0,
        EnabledApiLayerNames = null,
    };

    var extensions = new List<string>();
    if (supportedExtensions.Contains("XR_KHR_D3D11_enable")) { extensions.Add("XR_KHR_D3D11_enable"); } else {
        Log.Error("[{LogTag}] XR_KHR_D3D11_enable extension not supported!", "OpenXR");
    }

    if (supportedExtensions.Contains("XR_FB_display_refresh_rate")) { extensions.Add("XR_FB_display_refresh_rate"); }

    if (supportedExtensions.Contains("XR_EXT_debug_utils")) { extensions.Add("XR_EXT_debug_utils"); }

    var instance = new Instance();

    var ansiExtensions = extensions.Select(e => Marshal.StringToHGlobalAnsi(e)).ToArray();
    fixed (IntPtr* fixedAnsiExtensions = ansiExtensions) {
        ici.EnabledExtensionCount = (uint) extensions.Count;
        ici.EnabledExtensionNames = (byte**) fixedAnsiExtensions;

        const string appname = "SLZ OpenXR Doctor";
        ici.ApplicationInfo = new ApplicationInfo() {
            ApiVersion = 1ul << 48,
            ApplicationVersion = 1,
            EngineVersion = 1,
        };
        Marshal.Copy(appname.ToCharArray(), 0, (IntPtr) ici.ApplicationInfo.ApplicationName, appname.Length);
        ici.ApplicationInfo.EngineName[0] = (byte) '\0';

        xr.CreateInstance(ici, ref instance);
        xr.CurrentInstance = instance;
    }

    // INSTANCE
    var instanceProperties = new InstanceProperties(StructureType.InstanceProperties);
    xr.GetInstanceProperties(instance, ref instanceProperties);
    var runtimeName = Marshal.PtrToStringUTF8((IntPtr) instanceProperties.RuntimeName);
    Log.Information("[{LogTag}][Instance] Runtime: Name={Name} Version={Version}", "OpenXR", runtimeName,
        instanceProperties.RuntimeVersion);

    // SYSTEM
    var systemGetInfo = new SystemGetInfo(StructureType.SystemGetInfo) {FormFactor = FormFactor.HeadMountedDisplay};
    ulong systemId = 0; // NOTE: THIS MAY BE ZERO IF STEAMVR IS OPEN BUT LOADED XR RUNTIME IS OCULUS'S
    xr.GetSystem((Instance) xr.CurrentInstance, &systemGetInfo, (ulong*) &systemId);
    Log.Information("[{LogTag}][System] Id={SystemId}", "OpenXR", systemId);

    var systemProperties = new SystemProperties(StructureType.SystemProperties);
    xr.GetSystemProperties(instance, systemId, ref systemProperties);
    var systemName = Marshal.PtrToStringUTF8((IntPtr) systemProperties.SystemName);
    Log.Information("[{LogTag}][System] Name={Name}", "OpenXR", systemName);
    
    foreach (var ansiExtension in ansiExtensions) { Marshal.FreeHGlobal(ansiExtension); }
}

#endregion