using System.Management;
using Serilog;

public static class HardwareDiagnostics {
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
                    Log.Information("Found headset: {Name} {DeviceID}", data, deviceId);
                    headsets[data] = deviceId;
                }
            }
            {
                if (dataProp is {Value: string data and "Quest 2"} &&
                    deviceIdProp is {Value: string deviceId}) {
                    headsets[data] = deviceId;
                    Log.Information("Found headset: {Name} {DeviceID}", data, deviceId);
                }
            }
        }
    }
}