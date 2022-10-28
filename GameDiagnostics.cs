using System.Runtime.InteropServices;

namespace SLZ.XRDoctor;

public static class GameDiagnostics {
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(GameDiagnostics));
    private const string LogTag = "BONELAB";

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
        IntPtr hToken,
        out IntPtr pszPath);

    public static void Check() {
        var appDataPath = "";
        var pszPath = IntPtr.Zero;
        try {
            var result =
                SHGetKnownFolderPath(new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16"), 0, IntPtr.Zero, out pszPath);
            if (result >= 0) {
                var path = Marshal.PtrToStringAuto(pszPath);
                if (Directory.Exists(path)) { appDataPath = path; }
            }
        } finally {
            if (pszPath != IntPtr.Zero) { Marshal.FreeCoTaskMem(pszPath); }
        }

        if (!Directory.Exists(appDataPath)) {
            Log.Error("[{LogTag}]  Could not find AppData/LocalLow directory: {appDataPath}.", LogTag, appDataPath);
        }

        var gamePath = Path.Combine(appDataPath, "Stress Level Zero", "BONELAB");
        if (!Directory.Exists(appDataPath)) {
            Log.Error("[{LogTag}]  Could not find game's persistend data directory: {gamepath}.", LogTag, gamePath);
        }

        var playerLogPath = Path.Combine(gamePath, "Player.log");
        if (File.Exists(playerLogPath)) {
            var playerLog = File.ReadAllText(playerLogPath);
            Log.Debug("<<<<<<<<<<");
            Log.Debug("PLAYER LOG");
            Log.Debug("{LogContents}", playerLog);
            Log.Debug(">>>>>>>>>>");
        }

        var playerLogPrevPath = Path.Combine(gamePath, "Player-prev.log");
        if (File.Exists(playerLogPrevPath)) {
            var playerLogPrev = File.ReadAllText(playerLogPrevPath);
            Log.Debug("<<<<<<<<<<");
            Log.Debug("PLAYER LOG (PREV)");
            Log.Debug("{LogContents}", playerLogPrev);
            Log.Debug(">>>>>>>>>>");
        }

        var settingsJsonPath = Path.Combine(gamePath, "settings.json");
        if (File.Exists(settingsJsonPath)) {
            var settingsJson = File.ReadAllText(settingsJsonPath);
            Log.Debug("<<<<<<<<<<");
            Log.Debug("Settings");
            Log.Debug("{SettingsContents}", settingsJson);
            Log.Debug(">>>>>>>>>>");
        }

        var savesJsonPath = Path.Combine(gamePath, "Saves", "slot_0_save.json");
        if (File.Exists(savesJsonPath)) {
            var savesJson = File.ReadAllText(savesJsonPath);
            Log.Debug("<<<<<<<<<<");
            Log.Debug("Save");
            Log.Debug("{SaveContents}", savesJson);
            Log.Debug(">>>>>>>>>>");
        }
    }
}