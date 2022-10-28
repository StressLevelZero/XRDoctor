using System.Management;

namespace SLZ.XRDoctor;

public static class SystemDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(SystemDiagnostics));
    private const string LogTag = "System";

    public static void Check() {
        
        Log.Information(
            "[{LogTag}] OS Version: {OSVersion} ", LogTag, Environment.OSVersion);
        
        
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
        
        
        using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
        using (var objs = query.Get()) {
            foreach (var obj in objs) {
                try {
                    var addressWidth = (ushort) obj.GetPropertyValue("AddressWidth");
                    var architecture = (ushort) obj.GetPropertyValue("Architecture");
                    var availability = (ushort) obj.GetPropertyValue("Availability");
                    var dataWidth = (ushort) obj.GetPropertyValue("DataWidth");
                    var extClock = (uint) obj.GetPropertyValue("ExtClock");
                    var family = (ushort) obj.GetPropertyValue("Family");
                    var l2CacheSize = (uint) obj.GetPropertyValue("L2CacheSize");
                    var l3CacheSize = (uint) obj.GetPropertyValue("L3CacheSize");
                    var level = (ushort) obj.GetPropertyValue("Level");
                    var manufacturer = (string) obj.GetPropertyValue("Manufacturer");
                    var maxClockSpeed = (uint) obj.GetPropertyValue("MaxClockSpeed");
                    var name = (string) obj.GetPropertyValue("Name");
                    var numberOfCores = (uint) obj.GetPropertyValue("NumberOfCores");
                    var numberOfEnabledCore = (uint) obj.GetPropertyValue("NumberOfEnabledCore");
                    var numberOfLogicalProcessors = (uint) obj.GetPropertyValue("NumberOfLogicalProcessors");
                    var otherFamilyDescription = (string) obj.GetPropertyValue("OtherFamilyDescription");
                    var partNumber = (string) obj.GetPropertyValue("PartNumber");
                    string revision;
                    try {
                        revision = obj.GetPropertyValue("Revision").ToString();
                    } catch (Exception e) {
                        Log.Error(e, "Exception logging CPU revision.");
                        revision = "<error>";
                    }
                    var secondLevelAddressTranslationExtensions =
                        (bool) obj.GetPropertyValue("SecondLevelAddressTranslationExtensions");
                    var socketDesignation = (string) obj.GetPropertyValue("SocketDesignation");
                    var stepping = (string) obj.GetPropertyValue("Stepping");
                    var threadCount = (uint) obj.GetPropertyValue("ThreadCount");
                    var version = (string) obj.GetPropertyValue("Version");
                    var virtualizationFirmwareEnabled = (bool) obj.GetPropertyValue("VirtualizationFirmwareEnabled");
                    var vMMonitorModeExtensions = (bool) obj.GetPropertyValue("VMMonitorModeExtensions");

                    manufacturer = manufacturer == null ? "<null>" : $"\"{manufacturer.TrimEnd()}\"";
                    name = name == null ? "<null>" : $"\"{name.TrimEnd()}\"";
                    otherFamilyDescription = otherFamilyDescription == null
                        ? "<null>"
                        : $"\"{otherFamilyDescription.TrimEnd()}\"";
                    partNumber = partNumber == null ? "<null>" : $"\"{partNumber.TrimEnd()}\"";
                    socketDesignation = socketDesignation == null ? "<null>" : $"\"{socketDesignation.TrimEnd()}\"";
                    stepping = stepping == null ? "<null>" : $"\"{stepping.TrimEnd()}\"";
                    version = version == null ? "<null>" : $"\"{version.TrimEnd()}\"";

                    Log.Information(
                        "[{LogTag}] Processor AddressWidth={AddressWidth} Architecture={Architecture} Availability={Availability} DataWidth={DataWidth} ExtClock={ExtClock} Family={Family} L2CacheSize={L2CacheSize} L3CacheSize={L3CacheSize} Level={Level} Manufacturer={Manufacturer} MaxClockSpeed={MaxClockSpeed} Name={Name} NumberOfCores={NumberOfCores} NumberOfEnabledCore={NumberOfEnabledCore} NumberOfLogicalProcessors={NumberOfLogicalProcessors} OtherFamilyDescription={OtherFamilyDescription} PartNumber={PartNumber} Revision={Revision} SecondLevelAddressTranslationExtensions={SecondLevelAddressTranslationExtensions} SocketDesignation={SocketDesignation} Stepping={Stepping} ThreadCount={ThreadCount} Version={Version} VirtualizationFirmwareEnabled={VirtualizationFirmwareEnabled} VMMonitorModeExtensions={VMMonitorModeExtensions}",
                        LogTag, addressWidth, architecture, availability, dataWidth, extClock, family, l2CacheSize,
                        l3CacheSize, level, manufacturer, maxClockSpeed, name, numberOfCores, numberOfEnabledCore,
                        numberOfLogicalProcessors, otherFamilyDescription, partNumber, revision,
                        secondLevelAddressTranslationExtensions, socketDesignation, stepping, threadCount, version,
                        virtualizationFirmwareEnabled, vMMonitorModeExtensions);
                } catch (Exception e) {
                    Log.Error(e, "Error logging CPU information.");
                }
            }
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