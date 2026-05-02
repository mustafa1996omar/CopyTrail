using System.IO;
using CopyTrail.Data;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CopyTrail.Tests;

public sealed class CleanupRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly CopyTrailDbContext _context;
    private readonly ClipboardRepository _repository;

    public CleanupRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"CopyTrail_cleanup_test_{Guid.NewGuid():N}.db");
        _context = new CopyTrailDbContext(_dbPath);
        DatabaseInitializer.Initialize(_context);
        _repository = new ClipboardRepository(_context);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        foreach (var related in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
        {
            if (File.Exists(related))
                try { File.Delete(related); } catch { }
        }
    }

    // ── DeleteContentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteContentAsync_RemovesContentAndEvents()
    {
        var content = MakeContent("delete me", "hash_del1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.DeleteContentAsync(content.Id);

        var items = await _repository.GetRecentTimelineItemsAsync(100);
        Assert.Empty(items);
    }

    [Fact]
    public async Task DeleteContentAsync_OnlyRemovesSpecifiedItem()
    {
        var c1 = MakeContent("keep me", "hash_keep1");
        var c2 = MakeContent("delete me", "hash_del2");
        await _repository.AddClipboardContentAsync(c1);
        await _repository.AddClipboardEventAsync(MakeEvent(c1.Id));
        await _repository.AddClipboardContentAsync(c2);
        await _repository.AddClipboardEventAsync(MakeEvent(c2.Id));

        await _repository.DeleteContentAsync(c2.Id);

        var items = await _repository.GetRecentTimelineItemsAsync(100);
        Assert.Single(items);
        Assert.Equal("hash_keep1", items[0].Content.ContentHash);
    }

    // ── EnforceCountLimitAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EnforceCountLimitAsync_NoopWhenUnderLimit()
    {
        await InsertUnpinnedAsync("h1");
        await InsertUnpinnedAsync("h2");

        var deleted = await _repository.EnforceCountLimitAsync(maxCount: 5);

        Assert.Empty(deleted);
        var items = await _repository.GetRecentTimelineItemsAsync(100);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task EnforceCountLimitAsync_DeletesOldestWhenOverLimit()
    {
        // Insert 5 items in order; oldest has hash "h1".
        for (int i = 1; i <= 5; i++)
            await InsertUnpinnedAsync($"h{i}", DateTime.UtcNow.AddMinutes(-5 + i));

        var deleted = await _repository.EnforceCountLimitAsync(maxCount: 3);

        Assert.Equal(2, deleted.Count);
        var remaining = await _repository.GetRecentTimelineItemsAsync(100);
        Assert.Equal(3, remaining.Count);
        Assert.DoesNotContain(remaining, r => r.Content.ContentHash == "h1");
        Assert.DoesNotContain(remaining, r => r.Content.ContentHash == "h2");
    }

    [Fact]
    public async Task EnforceCountLimitAsync_NeverDeletesPinnedItems()
    {
        // Insert 3 pinned and 2 unpinned; limit = 2 so 3 must be deleted.
        // But pinned items should be kept, so only 1 unpinned gets deleted
        // and the count of unpinned goes to 1 (< limit).
        await InsertPinnedAsync("pinned1", DateTime.UtcNow.AddMinutes(-10));
        await InsertPinnedAsync("pinned2", DateTime.UtcNow.AddMinutes(-9));
        await InsertPinnedAsync("pinned3", DateTime.UtcNow.AddMinutes(-8));
        await InsertUnpinnedAsync("unpinned1", DateTime.UtcNow.AddMinutes(-7));
        await InsertUnpinnedAsync("unpinned2", DateTime.UtcNow.AddMinutes(-6));

        // Limit of 2 means we have 2 too many unpinned (we have 2, limit allows 2).
        // Actually with limit=2, unpinned count=2, so no deletion needed.
        // Let's use limit=1 to force deletion of 1 unpinned.
        var deleted = await _repository.EnforceCountLimitAsync(maxCount: 1);

        Assert.Single(deleted);
        var remaining = await _repository.GetRecentTimelineItemsAsync(100);
        // 3 pinned + 1 unpinned = 4
        Assert.Equal(4, remaining.Count);
        Assert.All(remaining.Where(r => !r.Content.IsPinned), r =>
            Assert.Equal("unpinned2", r.Content.ContentHash));
    }

    [Fact]
    public async Task EnforceCountLimitAsync_ReturnsImagePathsOfDeletedItems()
    {
        var c = MakeContent("with image", "hash_img1");
        c.ImagePath = "/fake/path/image.png";
        c.ThumbnailPath = "/fake/path/thumb.png";
        await _repository.AddClipboardContentAsync(c);
        await _repository.AddClipboardEventAsync(MakeEvent(c.Id));
        await InsertUnpinnedAsync("extra1");

        var deleted = await _repository.EnforceCountLimitAsync(maxCount: 1);

        Assert.Single(deleted);
        Assert.Equal("/fake/path/image.png", deleted[0].ImagePath);
        Assert.Equal("/fake/path/thumb.png", deleted[0].ThumbnailPath);
    }

    // ── EnforceStorageLimitAsync ──────────────────────────────────────────────

    [Fact]
    public async Task EnforceStorageLimitAsync_NoopWhenUnderLimit()
    {
        var c = MakeContent("small", "hash_sm1", sizeBytes: 100);
        await _repository.AddClipboardContentAsync(c);
        await _repository.AddClipboardEventAsync(MakeEvent(c.Id));

        var deleted = await _repository.EnforceStorageLimitAsync(maxStorageBytes: 1000);

        Assert.Empty(deleted);
    }

    [Fact]
    public async Task EnforceStorageLimitAsync_DeletesOldestWhenOverLimit()
    {
        // Two items each 600 bytes; limit 800 bytes → delete oldest (total 1200 > 800).
        var c1 = MakeContent("old", "hash_old1", sizeBytes: 600);
        var c2 = MakeContent("new", "hash_new1", sizeBytes: 600);
        await _repository.AddClipboardContentAsync(c1);
        await _repository.AddClipboardEventAsync(MakeEvent(c1.Id, DateTime.UtcNow.AddMinutes(-5)));
        await _repository.AddClipboardContentAsync(c2);
        await _repository.AddClipboardEventAsync(MakeEvent(c2.Id, DateTime.UtcNow));

        var deleted = await _repository.EnforceStorageLimitAsync(maxStorageBytes: 800);

        Assert.Single(deleted);
        var remaining = await _repository.GetRecentTimelineItemsAsync(100);
        Assert.Single(remaining);
        Assert.Equal("hash_new1", remaining[0].Content.ContentHash);
    }

    [Fact]
    public async Task EnforceStorageLimitAsync_NeverDeletesPinnedItems()
    {
        // Pinned item is large (900 bytes); unpinned is small (200 bytes); limit 500 bytes.
        // Only unpinned total (200) is checked; no deletion needed.
        var pinned = MakeContent("pinned large", "hash_pin_lg", sizeBytes: 900);
        pinned.IsPinned = true;
        await _repository.AddClipboardContentAsync(pinned);
        await _repository.AddClipboardEventAsync(MakeEvent(pinned.Id));
        await _repository.PinContentAsync(pinned.Id);

        var unpinned = MakeContent("unpinned small", "hash_unpin_sm", sizeBytes: 200);
        await _repository.AddClipboardContentAsync(unpinned);
        await _repository.AddClipboardEventAsync(MakeEvent(unpinned.Id));

        var deleted = await _repository.EnforceStorageLimitAsync(maxStorageBytes: 500);

        Assert.Empty(deleted);
        var remaining = await _repository.GetRecentTimelineItemsAsync(100);
        Assert.Equal(2, remaining.Count);
    }

    // ── GetAllKnownImagePathsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetAllKnownImagePathsAsync_ReturnsAllImagePaths()
    {
        var c1 = MakeContent("img1", "hash_ki1");
        c1.ImagePath = "/media/img1.png";
        c1.ThumbnailPath = "/media/thumb1.png";
        await _repository.AddClipboardContentAsync(c1);

        var c2 = MakeContent("no image", "hash_ki2");
        await _repository.AddClipboardContentAsync(c2);

        var paths = await _repository.GetAllKnownImagePathsAsync();

        Assert.Equal(2, paths.Count);
        Assert.Contains("/media/img1.png", paths);
        Assert.Contains("/media/thumb1.png", paths);
    }

    [Fact]
    public async Task GetAllKnownImagePathsAsync_EmptyWhenNoImages()
    {
        await InsertUnpinnedAsync("hash_noimg");

        var paths = await _repository.GetAllKnownImagePathsAsync();

        Assert.Empty(paths);
    }

    // ── GetUnpinnedImagePathsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetUnpinnedImagePathsAsync_ExcludesPinnedImagePaths()
    {
        var pinned = MakeContent("pinned img", "hash_pinimg");
        pinned.ImagePath = "/media/pinned.png";
        pinned.IsPinned = true;
        await _repository.AddClipboardContentAsync(pinned);
        await _repository.PinContentAsync(pinned.Id);

        var unpinned = MakeContent("unpinned img", "hash_unpinimg");
        unpinned.ThumbnailPath = "/media/unpin_thumb.png";
        await _repository.AddClipboardContentAsync(unpinned);

        var paths = await _repository.GetUnpinnedImagePathsAsync();

        Assert.Single(paths);
        Assert.Contains("/media/unpin_thumb.png", paths);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task InsertUnpinnedAsync(string hash, DateTime? createdAt = null)
    {
        var content = MakeContent(hash, hash, createdAt: createdAt);
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id, createdAt));
    }

    private async Task InsertPinnedAsync(string hash, DateTime? createdAt = null)
    {
        var content = MakeContent(hash, hash, createdAt: createdAt);
        content.IsPinned = true;
        await _repository.AddClipboardContentAsync(content);
        await _repository.PinContentAsync(content.Id);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id, createdAt));
    }

    private static ClipboardContent MakeContent(string text, string hash, long sizeBytes = 10, DateTime? createdAt = null) =>
        new()
        {
            ContentHash = hash,
            Kind = ClipboardItemKind.Text,
            PlainText = text,
            SizeBytes = sizeBytes,
            CreatedAtUtc = createdAt ?? DateTime.UtcNow,
        };

    private static ClipboardEvent MakeEvent(long contentId, DateTime? copiedAt = null) =>
        new()
        {
            ClipboardContentId = contentId,
            SourceKind = ClipboardSourceKind.App,
            CopiedAtUtc = copiedAt ?? DateTime.UtcNow,
            LastCopiedAtUtc = copiedAt ?? DateTime.UtcNow,
            CopyCount = 1,
        };
}
