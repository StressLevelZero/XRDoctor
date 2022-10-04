using System.Management;
using Serilog;

public static class SystemDiagnostics {
    private const string LogTag = "System";

    public static void Check() {
        foreach (var drive in DriveInfo.GetDrives()) {
            if (!drive.IsReady) { continue; }

            if (drive.DriveType != DriveType.Fixed) { continue; }

            var availableFreeSpace = drive.AvailableFreeSpace;
            var availableFreeSpaceGiB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);

            var totalFreeSpace = drive.TotalFreeSpace;
            var totalFreeSpaceGiB = drive.TotalFreeSpace / (1024 * 1024 * 1024);

            var totalSize = drive.TotalSize;
            var totalSizeGiB = drive.TotalSize / (1024 * 1024 * 1024);

            Log.Information(
                "[{LogTag}] Drive Name={name} Filesystem={filesystem}, " +
                "Available Space={availableFreeSpace}, ({availableFreeSpaceGiB} GiB) " +
                "Total Free Space={totalFreeSpace} ({totalFreeSpaceGiB} GiB), " +
                "Total Size={totalSize} ({totalSizeGiB} GiB)",
                LogTag, drive.Name, drive.DriveFormat, availableFreeSpace, availableFreeSpaceGiB, totalFreeSpace,
                totalFreeSpaceGiB, totalSize, totalSizeGiB);
        }

        using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var bankLabel = obj.GetPropertyValue("BankLabel");
                var deviceLocator = obj.GetPropertyValue("DeviceLocator");
                var tag = obj.GetPropertyValue("Tag");
                var part = obj.GetPropertyValue("PartNumber");
                var capacity = obj.GetPropertyValue("Capacity");
                var capacityGiB = long.Parse(capacity.ToString()) / (1024 * 1024 * 1024);
                var configuredClockSpeed = obj.GetPropertyValue("ConfiguredClockSpeed");
                var speed = obj.GetPropertyValue("Speed");
                var configuredVoltage = obj.GetPropertyValue("ConfiguredVoltage");
                var minVoltage = obj.GetPropertyValue("MinVoltage");
                var maxVoltage = obj.GetPropertyValue("MaxVoltage");

                Log.Information(
                    "[{LogTag}] Physical Memory: BankLabel=\"{bankLabel}\" DeviceLocator=\"{deviceLocator}\" Tag=\"{tag}\" Part=\"{part}\" " +
                    "Capacity={capacity} ({capacityGiB} GiB) ConfiguredClockSpeed={configuredClockSpeed} Speed={speed} " +
                    "ConfiguredVoltage={configuredVoltage} minVoltage={minVoltage} maxVoltage={maxVoltage}", LogTag,
                    bankLabel, deviceLocator, tag, part, capacity, capacityGiB, configuredClockSpeed, speed,
                    configuredVoltage,
                    minVoltage, maxVoltage);
            }
        }

        using (var query = new ManagementObjectSearcher("SELECT AllocatedBaseSize FROM Win32_PageFileUsage"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var allocatedBaseSize = (uint) obj.GetPropertyValue("AllocatedBaseSize");
                Log.Information("[{LogTag}] PageFileUsage AllocatedBaseSize={allocatedBaseSize}", LogTag,
                    allocatedBaseSize);
            }
        }

        using (var query = new ManagementObjectSearcher("SELECT MaximumSize FROM Win32_PageFileSetting"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                var maximumSize = (uint) obj.GetPropertyValue("MaximumSize");
                Log.Information("[{LogTag}] PageFileSetting MaximumSize={maximumSize}", LogTag, maximumSize);
            }
        }
    }
}