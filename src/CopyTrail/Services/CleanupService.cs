using System.IO;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;

namespace CopyTrail.Services;

public sealed class CleanupService
{
    private readonly ClipboardRepository _repository;
    private readonly FileStorageService _fileStorage;

    public CleanupService(ClipboardRepository repository, FileStorageService fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task DeleteItemAsync(long contentId, string? imagePath, string? thumbnailPath)
    {
        await _repository.DeleteContentAsync(contentId).ConfigureAwait(false);
        _fileStorage.DeleteMediaFileIfExists(imagePath);
        _fileStorage.DeleteMediaFileIfExists(thumbnailPath);
    }

    public async Task RunAfterCaptureAsync(AppSettings settings)
    {
        var deletedByCount = await _repository.EnforceCountLimitAsync(settings.MaxHistoryCount).ConfigureAwait(false);
        DeleteFiles(deletedByCount);

        var deletedByStorage = await _repository.EnforceStorageLimitAsync(settings.MaxStorageBytes).ConfigureAwait(false);
        DeleteFiles(deletedByStorage);
    }

    public async Task RunStartupCleanupAsync()
    {
        var knownPaths = await _repository.GetAllKnownImagePathsAsync().ConfigureAwait(false);
        var knownSet = new HashSet<string>(knownPaths, StringComparer.OrdinalIgnoreCase);

        string mediaRoot = _fileStorage.GetMediaRoot();
        if (!Directory.Exists(mediaRoot)) return;

        foreach (string file in Directory.GetFiles(mediaRoot, "*.*", SearchOption.AllDirectories))
        {
            if (!knownSet.Contains(file))
                _fileStorage.DeleteMediaFileIfExists(file);
        }
    }

    private void DeleteFiles(IReadOnlyList<(string? ImagePath, string? ThumbnailPath)> deletedPaths)
    {
        foreach (var (img, thumb) in deletedPaths)
        {
            _fileStorage.DeleteMediaFileIfExists(img);
            _fileStorage.DeleteMediaFileIfExists(thumb);
        }
    }
}
