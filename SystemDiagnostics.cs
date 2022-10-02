using System.Management;
using Serilog;

public static class SystemDiagnostics {
    private const string LogTag = "System";

    public static void Check() {
        foreach (var drive in DriveInfo.GetDrives()) {
            if (!drive.IsReady) { continue; }

            if (drive.DriveType != DriveType.Fixed) { continue; }

            Log.Information(
                "[{LogTag}] Drive Name={name} Filesystem={filesystem}, Available Space={available}, Total Free Space={free}, Total Size={total}",
                LogTag, drive.Name, drive.DriveFormat, drive.AvailableFreeSpace, drive.TotalFreeSpace, drive.TotalSize);
        }

        using (var query = new ManagementObjectSearcher("SELECT AllocatedBaseSize FROM Win32_PageFileUsage")) {
            foreach (var obj in query.Get()) {
                var allocatedBaseSize = (uint) obj.GetPropertyValue("AllocatedBaseSize");
                Log.Information("[{LogTag}] PageFileUsage AllocatedBaseSize={allocatedBaseSize}", LogTag, allocatedBaseSize);
            }
        }

        using (var query = new ManagementObjectSearcher("SELECT MaximumSize FROM Win32_PageFileSetting")) {
            foreach (var obj in query.Get()) {
                var maximumSize = (uint) obj.GetPropertyValue("MaximumSize");
                Log.Information("[{LogTag}] PageFileSetting MaximumSize={maximumSize}", maximumSize);
            }
        }
    }
}