namespace CopyTrail.Models;

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    public string OpenPopupShortcut { get; set; } = "Alt+V";
    public bool StartWithWindows { get; set; } = false;
    public bool StoreImages { get; set; } = true;

    // 10 MB
    public long MaxTextBytes { get; set; } = 10 * 1024 * 1024;

    // 20 MB
    public long MaxImageBytes { get; set; } = 20 * 1024 * 1024;

    public bool CollapseRapidDuplicates { get; set; } = true;
    public int RapidDuplicateWindowSeconds { get; set; } = 5;

    public List<string> ExcludedProcessNames { get; set; } =
    [
        "1Password.exe",
        "Bitwarden.exe",
        "KeePass.exe",
        "KeePassXC.exe",
        "CredentialUIBroker.exe"
    ];

    public int MaxHistoryCount { get; set; } = 10_000;

    // 1 GB
    public long MaxStorageBytes { get; set; } = 1024L * 1024 * 1024;
}
