using System.Management;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SLZ.XRDoctor;

public static class HardwareDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(HardwareDiagnostics));
    public static void FindHeadsets(out Dictionary<string, string> headsets) {
        headsets = new Dictionary<string, string>();

        var mos = new ManagementObjectSearcher(null, @"SELECT * FROM Win32_PnPEntity");
        foreach (var mo in mos.Get().OfType<ManagementObject>()) {
            var args = new object[] {new string[] {"DEVPKEY_Device_BusReportedDeviceDesc"}, null};
            mo.InvokeMethod("GetDeviceProperties", args);
            var mbos = (ManagementBaseObject[]) args[1];
            if (mbos.Length == 0) { continue; }

            var properties = mbos[0].Properties.OfType<PropertyData>();

            var dataProp = properties.FirstOrDefault(p => p.Name == "Data");
            var deviceIdProp = properties.FirstOrDefault(p => p.Name == "DeviceID");

            // Kinda overspecified, but whatever.
            {
                if (dataProp is {Value: string data and "Index HMD"} &&
                    deviceIdProp is {Value: string deviceId}) {
                    Log.Information("[{LogTag}] Found headset: {Name} {DeviceID}", "Hardware", data, deviceId);
                    headsets[data] = deviceId;
                }
            }
            {
                if (dataProp is {Value: string data and "Quest 2"} &&
                    deviceIdProp is {Value: string deviceId}) {
                    headsets[data] = deviceId;
                    Log.Information("[{LogTag}] Found headset: {Name} {DeviceID}", "Hardware", data, deviceId);
                }
            }
        }
    }

    public static void ListHardware()
    {
        using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                try {
                    var json = Collection2Json(obj.Properties);
                    Log.Information("[{LogTag}] {json}", "Processor", json.ToJsonString());
                } catch (Exception e) {
                    Log.Error(e, "Error logging CPU information.");
                }
            }
        }
        
        using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var json = Collection2Json(obj.Properties);
                Log.Information("[{LogTag}] {json}", "PhysicalMemory", json.ToJsonString());
            }
        }
        
        using (var query = new ManagementObjectSearcher("SELECT AllocatedBaseSize FROM Win32_PageFileUsage"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var allocatedBaseSize = (uint)obj.GetPropertyValue("AllocatedBaseSize");
                Log.Information("[{LogTag}] PageFileUsage AllocatedBaseSize={allocatedBaseSize}", "PageFileUsage", allocatedBaseSize);
            }
        }

        using (var query = new ManagementObjectSearcher("SELECT MaximumSize FROM Win32_PageFileSetting"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var maximumSize = (uint)obj.GetPropertyValue("MaximumSize");
                Log.Information("[{LogTag}] PageFileSetting MaximumSize={maximumSize}", "PageFileSetting",
                    maximumSize);
            }
        }
        
        using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var json = Collection2Json(obj.Properties);
                Log.Information("[{LogTag}] {json}", "DiskDrive", json.ToJsonString());
            }
        }

        using (var query = new ManagementObjectSearcher("SELECT * FROM MSFT_PhysicalDisk"))
        {
            var scope = new ManagementScope(@"\\.\root\microsoft\windows\storage");
            scope.Connect();
            query.Scope = scope;
            query.Options.UseAmendedQualifiers = true;
            
            using (var objs = query.Get()) {
                foreach (var obj in objs) {
                    var json = Collection2Json(obj.Properties);
                    Log.Information("[{LogTag}] {json}", "PhysicalDisk", json.ToJsonString());
                }
            }
        }
        
    }

    private static JsonObject Collection2Json(PropertyDataCollection propertyData) {
        var json = new JsonObject();
        foreach (var property in propertyData)
        {
            json[property.Name] = property.Value switch {
                ushort us => us,
                short s => s,
                uint ui => ui,
                int i => i,
                ulong ul => ul,
                long l => l,
                float f => f,
                double d => d,
                ushort[] usa => Array2JsonArray(usa),
                short[] sa => Array2JsonArray(sa),
                uint[] uia => Array2JsonArray(uia),
                int[] ia => Array2JsonArray(ia),
                ulong[] ula => Array2JsonArray(ula),
                long[] la => Array2JsonArray(la),
                float[] fa => Array2JsonArray(fa),
                double[] da => Array2JsonArray(da),
                string[] sa => Array2JsonArray(sa),
                { } o => o.ToString(),
                _ => null,
            };
            
            // Redact serials
            if (property.Name.Contains("serial", StringComparison.InvariantCultureIgnoreCase)) {
                json[property.Name] = "not tracked";
            }
        }

        return json;
    }

    private static JsonArray Array2JsonArray<TType> (TType[] array) {
        var jsonArray = new JsonArray();
        foreach (var item in array) {
            jsonArray.Add(item);
        }
        return jsonArray;
    }
    
}