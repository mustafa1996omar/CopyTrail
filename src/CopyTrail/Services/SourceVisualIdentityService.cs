using CopyTrail.Models;
using CopyTrail.Utilities;

namespace CopyTrail.Services;

// Resolves SourceVisualIdentity with icon paths and derived colors.
// Use GetIdentity in preference to AppNameMapper.Resolve wherever processPath is available.
public sealed class SourceVisualIdentityService
{
    private readonly AppIconService _iconService;

    public SourceVisualIdentityService(AppIconService iconService)
    {
        _iconService = iconService;
    }

    // Returns an identity with IconPath and, for unknown apps, a hash-derived accent color.
    // Icon extraction is queued in the background if the icon isn't cached yet.
    public SourceVisualIdentity GetIdentity(string? processName, string? processPath)
    {
        var base_ = AppNameMapper.Resolve(processName);
        string? iconPath = null;

        if (!string.IsNullOrWhiteSpace(processPath))
            iconPath = _iconService.GetOrQueueIconPath(processName!, processPath);
        else if (!string.IsNullOrWhiteSpace(processName))
            iconPath = _iconService.GetCachedIconPath(processName, null);

        // For unknown apps (those not in the known registry), attempt a derived accent.
        string accentHex = base_.AccentColorHex;
        string softHex = base_.SoftAccentColorHex;
        string fgHex = base_.ForegroundColorHex;

        if (!AppNameMapper.IsKnown(processName) && !string.IsNullOrWhiteSpace(processName))
        {
            // Try icon-derived color first, fall back to name hash.
            string? iconAccent = iconPath is not null
                ? ColorUtilities.TryGetDominantColorFromFile(iconPath)
                : _iconService.TryGetIconDerivedAccent(processName, processPath);

            accentHex = iconAccent ?? ColorUtilities.GenerateAccentHex(processName);
            softHex = ColorUtilities.GenerateSoftHex(accentHex);
            fgHex = ColorUtilities.GetReadableForegroundHex(accentHex);
        }

        // Return early if nothing changed and no icon to attach.
        if (iconPath is null &&
            accentHex == base_.AccentColorHex &&
            softHex == base_.SoftAccentColorHex &&
            fgHex == base_.ForegroundColorHex)
        {
            return base_;
        }

        return new SourceVisualIdentity
        {
            AppName = base_.AppName,
            ProcessName = base_.ProcessName,
            IconPath = iconPath,
            AccentColorHex = accentHex,
            SoftAccentColorHex = softHex,
            ForegroundColorHex = fgHex,
            Initial = base_.Initial,
        };
    }
}
