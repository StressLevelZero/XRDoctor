namespace SLZ.XRDoctor;

public static class SystemDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(SystemDiagnostics));
    private const string LogTag = "System";

    public static void Check() {
        CheckTime();
        CheckSystem();
    }
    
    public static void CheckTime()
    {
        Log.Information(
            "[{LogTag}] Timezone ID: {TimezoneID} DisplayName: {DisplayName} StandardName: {StandardName} " +
            "DaylightName: {DaylightName} BaseUtcOffset: {BaseUtcOffset} " +
            "SupportsDaylightSavingTime: {SupportsDaylightSavingTime} IsDaylightSavingTime: {IsDaylightSavingTime}",
            LogTag,
            TimeZoneInfo.Local.Id, TimeZoneInfo.Local.DisplayName, TimeZoneInfo.Local.StandardName,
            TimeZoneInfo.Local.DaylightName, TimeZoneInfo.Local.BaseUtcOffset,
            TimeZoneInfo.Local.SupportsDaylightSavingTime, TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now));
    }

    public static void CheckSystem() {
        
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
    }
}