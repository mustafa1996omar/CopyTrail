namespace CopyTrail.Models;

public sealed class SourceVisualIdentity
{
    public string AppName { get; init; } = "Unknown";
    public string ProcessName { get; init; } = "";
    public string? IconPath { get; init; }
    public string AccentColorHex { get; init; } = "#64748B";
    public string SoftAccentColorHex { get; init; } = "#F1F5F9";
    public string ForegroundColorHex { get; init; } = "#1E293B";
    public string Initial { get; init; } = "?";

    public static SourceVisualIdentity Chrome { get; } = new()
    {
        AppName = "Google Chrome", ProcessName = "chrome",
        AccentColorHex = "#4285F4", SoftAccentColorHex = "#EAF2FF",
        ForegroundColorHex = "#1A3D7C", Initial = "C"
    };

    public static SourceVisualIdentity Edge { get; } = new()
    {
        AppName = "Microsoft Edge", ProcessName = "msedge",
        AccentColorHex = "#0078D7", SoftAccentColorHex = "#E6F3FF",
        ForegroundColorHex = "#004E8C", Initial = "E"
    };

    public static SourceVisualIdentity VSCode { get; } = new()
    {
        AppName = "VS Code", ProcessName = "Code",
        AccentColorHex = "#007ACC", SoftAccentColorHex = "#E6F5FF",
        ForegroundColorHex = "#004E84", Initial = "V"
    };

    public static SourceVisualIdentity Codex { get; } = new()
    {
        AppName = "Codex", ProcessName = "codex",
        AccentColorHex = "#10A37F", SoftAccentColorHex = "#E7F8F3",
        ForegroundColorHex = "#065F46", Initial = "C"
    };

    public static SourceVisualIdentity Word { get; } = new()
    {
        AppName = "Microsoft Word", ProcessName = "WINWORD",
        AccentColorHex = "#2B579A", SoftAccentColorHex = "#EAF1FB",
        ForegroundColorHex = "#1A3666", Initial = "W"
    };

    public static SourceVisualIdentity WindowsTerminal { get; } = new()
    {
        AppName = "Windows Terminal", ProcessName = "WindowsTerminal",
        AccentColorHex = "#111827", SoftAccentColorHex = "#F3F4F6",
        ForegroundColorHex = "#111827", Initial = "T"
    };

    public static SourceVisualIdentity FileExplorer { get; } = new()
    {
        AppName = "File Explorer", ProcessName = "explorer",
        AccentColorHex = "#F59E0B", SoftAccentColorHex = "#FFF7E6",
        ForegroundColorHex = "#78350F", Initial = "F"
    };

    public static SourceVisualIdentity Screenshot { get; } = new()
    {
        AppName = "Screenshot", ProcessName = "SnippingTool",
        AccentColorHex = "#2563EB", SoftAccentColorHex = "#EFF6FF",
        ForegroundColorHex = "#1E40AF", Initial = "S"
    };

    public static SourceVisualIdentity Figma { get; } = new()
    {
        AppName = "Figma", ProcessName = "figma",
        AccentColorHex = "#A259FF", SoftAccentColorHex = "#F5EDFF",
        ForegroundColorHex = "#5B21B6", Initial = "F"
    };

    public static SourceVisualIdentity Unknown { get; } = new()
    {
        AppName = "Unknown App", ProcessName = "",
        AccentColorHex = "#64748B", SoftAccentColorHex = "#F1F5F9",
        ForegroundColorHex = "#1E293B", Initial = "?"
    };
}
