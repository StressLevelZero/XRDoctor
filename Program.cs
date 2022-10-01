﻿using System.Runtime.InteropServices;
using Figgle;
using Serilog;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using SLZ.XRDoctor;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

var programName = FiggleFonts.Standard.Render("SLZ OpenXR Doctor");
Console.WriteLine(programName);

const string outputTemplate = "[{Timestamp:HH:mm:ss} {Level}] {Message:l}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.File("log_.txt", outputTemplate: outputTemplate, rollingInterval: RollingInterval.Day)
    .MinimumLevel.Debug()
    .CreateLogger();

OpenXRDiagnostics.FindActiveRuntime(out var XR_RUNTIME_JSON);
if (!string.IsNullOrWhiteSpace(XR_RUNTIME_JSON)) {
    Log.Information("Active Runtime (XR_RUNTIME_JSON): {XR_RUNTIME_JSON}", XR_RUNTIME_JSON);
} else { Log.Error("Could not find an active OpenXR runtime."); }

OpenXRDiagnostics.FindRegisteredRuntimes(out var runtimes);
foreach (var (runtimeName, manifest) in runtimes) {
    if (string.IsNullOrWhiteSpace(manifest.Runtime.Name)) {
        Log.Information("Available Runtime: {runtime} - {@manifest}", runtimeName, manifest);
    } else {
        Log.Information("Available Runtime: \"{name}\" - {runtime} - {@manifest}", manifest.Runtime.Name,
            runtimeName,
            manifest);
    }
}

OculusDiagnostics.CheckDirectly(out var hasOculus);
SteamVRDiagnostics.CheckDirectly(out var hasSteamVR);
ViveVRDiagnostics.CheckDirectly(out var hasViveVr);
WindowsMRDiagnostics.CheckDirectly(out var hasWindowsMR);

HardwareDiagnostics.FindHeadsets(out var headsets);

if (XR_RUNTIME_JSON.Contains("Steam", StringComparison.InvariantCultureIgnoreCase)) {
    SteamVRDiagnostics.CheckDevices();
}

#region OpenXR

Log.Information("Loading OpenXR.");
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
        Log.Information("[OpenXR] API Layer: Name={Name} Version={Version}", name, layer.LayerVersion);
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
        Log.Information("[Instance] Extension: Name={Name} Version={Version}", name, extensionProp.ExtensionVersion);
    }

    var ici = new InstanceCreateInfo(StructureType.InstanceCreateInfo) {
        EnabledApiLayerCount = 0,
        EnabledApiLayerNames = null,
    };

    var extensions = new List<string>();
    if (supportedExtensions.Contains("XR_KHR_D3D11_enable")) { extensions.Add("XR_KHR_D3D11_enable"); } else {
        Log.Error("XR_KHR_D3D11_enable extension not supported!");
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
    Log.Information("[Instance] Runtime: Name={Name} Version={Version}", runtimeName,
        instanceProperties.RuntimeVersion);

    // SYSTEM
    var systemGetInfo = new SystemGetInfo(StructureType.SystemGetInfo) {FormFactor = FormFactor.HeadMountedDisplay};
    ulong systemId = 0; // NOTE: THIS IS ZERO IF STEAMVR IS OPEN BUT LOADED XR RUNTIME IS OCULUS'S
    xr.GetSystem((Instance) xr.CurrentInstance, &systemGetInfo, (ulong*) &systemId);
    Log.Information("[System] Id={SystemId}", systemId);

    var systemProperties = new SystemProperties(StructureType.SystemProperties);
    xr.GetSystemProperties(instance, systemId, ref systemProperties);
    var systemName = Marshal.PtrToStringUTF8((IntPtr) systemProperties.SystemName);
    Log.Information("[System] Name={Name}", systemName);

    xr.TryGetInstanceExtension(null, instance, out KhrD3D11Enable khrD3D11);

    var d3d11Requirements = new GraphicsRequirementsD3D11KHR(StructureType.GraphicsRequirementsD3D11Khr);
    khrD3D11.GetD3D11GraphicsRequirements(instance, systemId, ref d3d11Requirements);
    Log.Information("[System] D3D11 Adapter LUID={LUID}", d3d11Requirements.AdapterLuid);

    var dxgiFactory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

    var found = false;
    var adapterIndex = 0;
    IDXGIAdapter adapter = null;

    while (!found && !dxgiFactory.EnumAdapters(adapterIndex, out adapter).Failure) {
        var luid = (ulong) (adapter.Description.Luid.HighPart << sizeof(uint)) + adapter.Description.Luid.LowPart;
        if (luid == d3d11Requirements.AdapterLuid) { found = true; }

        adapterIndex++;
    }

    ID3D11Device device = default;

    D3D11.D3D11CreateDevice(
        adapter,
        DriverType.Unknown,
        DeviceCreationFlags.None,
        new FeatureLevel[] {FeatureLevel.Level_11_1},
        out device,
        out var featureLevel,
        out var immediateContext);
    Log.Information("[D3D11] Feature Level = {FeatureLevel}", featureLevel);

    var d3d11Khr = new GraphicsBindingD3D11KHR(StructureType.GraphicsBindingD3D11Khr);
    d3d11Khr.Device = (void*) device.NativePointer;

    var sci = new SessionCreateInfo(StructureType.SessionCreateInfo);
    sci.SystemId = systemId;
    sci.Next = &d3d11Khr;

    var session = new Session();
    xr.CreateSession(instance, sci, ref session);

    foreach (var ansiExtension in ansiExtensions) { Marshal.FreeHGlobal(ansiExtension); }
}

#endregion