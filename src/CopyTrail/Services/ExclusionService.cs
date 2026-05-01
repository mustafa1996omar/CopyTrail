using CopyTrail.Models;

namespace CopyTrail.Services;

public sealed class ExclusionService
{
    // CopyTrail's own paste operations must never be recorded.
    private const string SelfProcessName = "CopyTrail";

    private readonly AppSettings _settings;

    public ExclusionService(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Returns true if clipboard events from <paramref name="processName"/> should be silently discarded.
    /// The check is case-insensitive and tolerates ".exe" suffixes in either the stored list or the
    /// supplied process name so that entries like "1Password.exe" match the runtime name "1Password".
    /// </summary>
    public bool IsExcluded(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;

        string normalized = StripExe(processName);

        if (string.Equals(normalized, SelfProcessName, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (string entry in _settings.ExcludedProcessNames)
        {
            if (string.Equals(StripExe(entry), normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string StripExe(string name) =>
        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
}
