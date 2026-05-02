using System.IO;

namespace CopyTrail.Services;

/// <summary>
/// Simple file-based logger for CopyTrail.
/// Log file: %LOCALAPPDATA%\CopyTrail\logs\CopyTrail.log
/// Max size: 5 MB — rotated to CopyTrail.log.old then started fresh.
/// Rules: never log clipboard content; only log event types, errors, app names, timestamps, exception types.
/// </summary>
public static class LoggingService
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CopyTrail", "logs");

    private static readonly string LogPath = Path.Combine(LogDir, "CopyTrail.log");
    private static readonly string OldLogPath = Path.Combine(LogDir, "CopyTrail.log.old");

    private const long MaxLogBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly object _lock = new();

    public static void LogError(string component, string message, Exception? ex = null)
    {
        string detail = ex is null
            ? message
            : $"{message} ({ex.GetType().Name})";
        Write("ERROR", component, detail);
    }

    public static void LogWarning(string component, string message)
    {
        Write("WARN", component, message);
    }

    public static void LogInfo(string component, string message)
    {
        Write("INFO", component, message);
    }

    private static void Write(string level, string component, string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level} {component}: {message}{Environment.NewLine}";

            lock (_lock)
            {
                EnsureLogDirectory();
                RotateIfNeeded();
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
            // Logging must never throw or crash the app.
        }
    }

    private static void EnsureLogDirectory()
    {
        if (!Directory.Exists(LogDir))
            Directory.CreateDirectory(LogDir);
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
            return;

        long size = new FileInfo(LogPath).Length;
        if (size < MaxLogBytes)
            return;

        // Overwrite the .old file and start fresh.
        try
        {
            if (File.Exists(OldLogPath))
                File.Delete(OldLogPath);
            File.Move(LogPath, OldLogPath);
        }
        catch
        {
            // If rotation fails, keep writing to the current file.
        }
    }
}
