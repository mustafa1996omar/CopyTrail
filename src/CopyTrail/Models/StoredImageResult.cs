namespace CopyTrail.Models;

public sealed class StoredImageResult
{
    public bool Success { get; init; }
    public string? ImagePath { get; init; }
    public string? ThumbnailPath { get; init; }
    public bool IsLarge { get; init; }
    public string? ErrorMessage { get; init; }

    public static StoredImageResult Failed(string reason) =>
        new() { Success = false, ErrorMessage = reason };
}
