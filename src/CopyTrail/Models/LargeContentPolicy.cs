namespace CopyTrail.Models;

public static class LargeContentPolicy
{
    public static bool IsLargeText(long sizeBytes, AppSettings settings) =>
        sizeBytes > settings.MaxTextBytes;

    public static bool IsLargeImage(long sizeBytes, AppSettings settings) =>
        sizeBytes > settings.MaxImageBytes;

    public static bool ShouldStoreImage(AppSettings settings) =>
        settings.StoreImages;

    public static bool IsExcludedProcess(string? processName, AppSettings settings) =>
        processName is not null &&
        settings.ExcludedProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase);

    public static bool ExceedsStorageLimit(long totalBytes, AppSettings settings) =>
        totalBytes > settings.MaxStorageBytes;
}
