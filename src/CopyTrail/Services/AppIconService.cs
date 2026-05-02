using System.IO;
using System.Security.Cryptography;
using System.Text;
using CopyTrail.Utilities;

namespace CopyTrail.Services;

public sealed class AppIconService
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CopyTrail", "icons");

    // Produces a safe, deterministic filename for the icon cache.
    public static string GetCacheFileName(string processName, string? processPath)
    {
        string safeName = SanitizeName(processName);

        if (!string.IsNullOrEmpty(processPath))
        {
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(processPath.ToLowerInvariant()));
            string shortHash = Convert.ToHexString(hash)[..8].ToLowerInvariant();
            return $"{safeName}_{shortHash}.png";
        }

        return $"{safeName}.png";
    }

    // Returns the full path to a cached icon if it exists, otherwise null.
    public string? GetCachedIconPath(string processName, string? processPath)
    {
        string fileName = GetCacheFileName(processName, processPath);
        string fullPath = Path.Combine(CacheDir, fileName);
        return File.Exists(fullPath) ? fullPath : null;
    }

    // Fires background extraction without blocking the caller.
    // Failures are logged via Debug but never propagate.
    public void QueueExtraction(string processName, string? processPath)
    {
        if (string.IsNullOrWhiteSpace(processPath) || string.IsNullOrWhiteSpace(processName))
            return;

        _ = Task.Run(() => ExtractAndCacheAsync(processName, processPath));
    }

    // Extracts the icon from the process executable and saves it as a PNG.
    // Returns the cached file path on success, null on failure.
    public async Task<string?> ExtractAndCacheAsync(string processName, string? processPath)
    {
        if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
            return null;

        try
        {
            string fileName = GetCacheFileName(processName, processPath);
            string fullPath = Path.Combine(CacheDir, fileName);

            if (File.Exists(fullPath))
                return fullPath;

            Directory.CreateDirectory(CacheDir);

            var icon = await Task.Run(() =>
                System.Drawing.Icon.ExtractAssociatedIcon(processPath));

            if (icon is null) return null;

            using var bmp = icon.ToBitmap();
            using var resized = new System.Drawing.Bitmap(bmp, 32, 32);
            resized.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

            return fullPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[AppIconService] Failed to extract icon for '{processName}': {ex.GetType().Name}");
            LoggingService.LogError("AppIconService", $"Icon extraction failed for process '{processName}'", ex);
            return null;
        }
    }

    // Checks the cache synchronously, then queues background extraction if absent.
    // Returns cached path immediately (may be null on first encounter).
    public string? GetOrQueueIconPath(string processName, string? processPath)
    {
        string? cached = GetCachedIconPath(processName, processPath);
        if (cached is not null) return cached;
        QueueExtraction(processName, processPath);
        return null;
    }

    // Tries to derive an accent color from the cached icon.
    // Returns null if the icon isn't cached or color extraction fails.
    public string? TryGetIconDerivedAccent(string processName, string? processPath)
    {
        string? iconPath = GetCachedIconPath(processName, processPath);
        if (iconPath is null) return null;
        return ColorUtilities.TryGetDominantColorFromFile(iconPath);
    }

    private static string SanitizeName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.Length > 0 ? sb.ToString() : "app";
    }
}
