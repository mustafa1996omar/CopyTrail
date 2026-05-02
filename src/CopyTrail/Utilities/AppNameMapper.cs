using CopyTrail.Models;

namespace CopyTrail.Utilities;


public static class AppNameMapper
{
    private static readonly Dictionary<string, SourceVisualIdentity> KnownApps =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"]          = SourceVisualIdentity.Chrome,
            ["msedge"]          = SourceVisualIdentity.Edge,
            ["firefox"]         = new SourceVisualIdentity
            {
                AppName = "Firefox", ProcessName = "firefox",
                AccentColorHex = "#FF7139", SoftAccentColorHex = "#FFF0EB",
                ForegroundColorHex = "#C1340A", Initial = "F"
            },
            ["WINWORD"]         = SourceVisualIdentity.Word,
            ["OUTLOOK"]         = new SourceVisualIdentity
            {
                AppName = "Microsoft Outlook", ProcessName = "OUTLOOK",
                AccentColorHex = "#0072C6", SoftAccentColorHex = "#E5F2FB",
                ForegroundColorHex = "#004880", Initial = "O"
            },
            ["Code"]            = SourceVisualIdentity.VSCode,
            ["Cursor"]          = new SourceVisualIdentity
            {
                AppName = "Cursor", ProcessName = "Cursor",
                AccentColorHex = "#6B57FF", SoftAccentColorHex = "#F0EDFF",
                ForegroundColorHex = "#3B2BC2", Initial = "C"
            },
            ["codex"]           = SourceVisualIdentity.Codex,
            ["WindowsTerminal"] = SourceVisualIdentity.WindowsTerminal,
            ["powershell"]      = new SourceVisualIdentity
            {
                AppName = "PowerShell", ProcessName = "powershell",
                AccentColorHex = "#012456", SoftAccentColorHex = "#E5EAF3",
                ForegroundColorHex = "#012456", Initial = "P"
            },
            ["pwsh"]            = new SourceVisualIdentity
            {
                AppName = "PowerShell", ProcessName = "pwsh",
                AccentColorHex = "#012456", SoftAccentColorHex = "#E5EAF3",
                ForegroundColorHex = "#012456", Initial = "P"
            },
            ["explorer"]        = SourceVisualIdentity.FileExplorer,
            ["SnippingTool"]    = SourceVisualIdentity.Screenshot,
            ["mspaint"]         = new SourceVisualIdentity
            {
                AppName = "Paint", ProcessName = "mspaint",
                AccentColorHex = "#2563EB", SoftAccentColorHex = "#EFF6FF",
                ForegroundColorHex = "#1E40AF", Initial = "P"
            },
        };

    public static SourceVisualIdentity Resolve(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return SourceVisualIdentity.Unknown;

        if (KnownApps.TryGetValue(processName, out var known))
            return known;

        // Fallback: derive consistent accent color from process name hash.
        string accent = ColorUtilities.GenerateAccentHex(processName);
        string soft = ColorUtilities.GenerateSoftHex(accent);
        string fg = ColorUtilities.GetReadableForegroundHex(accent);

        return new SourceVisualIdentity
        {
            AppName = processName,
            ProcessName = processName,
            AccentColorHex = accent,
            SoftAccentColorHex = soft,
            ForegroundColorHex = fg,
            Initial = processName.Length > 0 ? char.ToUpperInvariant(processName[0]).ToString() : "?"
        };
    }

    public static bool IsKnown(string? processName) =>
        !string.IsNullOrWhiteSpace(processName) && KnownApps.ContainsKey(processName);
}
