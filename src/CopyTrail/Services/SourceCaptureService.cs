using System.Diagnostics;
using System.Text;
using CopyTrail.Helpers;
using CopyTrail.Models;
using CopyTrail.Utilities;

namespace CopyTrail.Services;

public sealed class SourceCaptureService
{
    private const int MaxWindowTitleLength = 512;

    public ClipboardSource Capture(ClipboardItemKind contentKind)
    {
        try
        {
            var hwnd = Win32Helpers.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return ClipboardSource.Unknown;

            var title = GetWindowTitle(hwnd);

            Win32Helpers.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0)
                return ClipboardSource.Unknown;

            return BuildSource(pid, title, contentKind);
        }
        catch
        {
            return ClipboardSource.Unknown;
        }
    }

    private static ClipboardSource BuildSource(uint pid, string? windowTitle, ClipboardItemKind contentKind)
    {
        try
        {
            using var process = Process.GetProcessById((int)pid);
            var processName = process.ProcessName;
            string? processPath = null;

            try { processPath = process.MainModule?.FileName; }
            catch { /* access denied on some elevated processes */ }

            var identity = AppNameMapper.Resolve(processName);
            var sourceKind = DetermineSourceKind(contentKind, processName);

            return new ClipboardSource
            {
                SourceAppName = identity.AppName,
                SourceProcessName = processName,
                SourceProcessPath = processPath,
                SourceWindowTitle = windowTitle,
                SourceKind = sourceKind,
                Identity = identity
            };
        }
        catch
        {
            return ClipboardSource.Unknown;
        }
    }

    private static string? GetWindowTitle(IntPtr hwnd)
    {
        try
        {
            var sb = new StringBuilder(MaxWindowTitleLength);
            int len = Win32Helpers.GetWindowText(hwnd, sb, MaxWindowTitleLength);
            return len > 0 ? sb.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static ClipboardSourceKind DetermineSourceKind(ClipboardItemKind contentKind, string processName) =>
        contentKind switch
        {
            ClipboardItemKind.Screenshot => ClipboardSourceKind.SystemScreenshot,
            ClipboardItemKind.FileReference => ClipboardSourceKind.SystemFileCopy,
            _ when string.IsNullOrWhiteSpace(processName) => ClipboardSourceKind.Unknown,
            _ => ClipboardSourceKind.App
        };
}
