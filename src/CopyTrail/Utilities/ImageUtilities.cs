using System.IO;

namespace CopyTrail.Utilities;

public static class ImageUtilities
{
    public static string GetMediaRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CopyTrail",
            "media");

    public static string GetThumbnailRoot() =>
        Path.Combine(GetMediaRoot(), "thumbs");

    public static string GetImagePath(string contentHash) =>
        Path.Combine(GetMediaRoot(), GetSafeFilename(contentHash) + ".png");

    public static string GetThumbnailPath(string contentHash) =>
        Path.Combine(GetThumbnailRoot(), GetSafeFilename(contentHash) + "_thumb.png");

    // Strips characters that are unsafe in file names; keeps alphanumerics, hyphens, underscores.
    public static string GetSafeFilename(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return "_empty";

        var chars = new System.Text.StringBuilder(hash.Length);
        foreach (char c in hash)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                chars.Append(c);
        }

        return chars.Length == 0 ? "_invalid" : chars.ToString();
    }
}
