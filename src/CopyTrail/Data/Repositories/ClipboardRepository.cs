using CopyTrail.Models;
using Microsoft.Data.Sqlite;

namespace CopyTrail.Data.Repositories;

public sealed class ClipboardRepository
{
    private readonly CopyTrailDbContext _context;

    public ClipboardRepository(CopyTrailDbContext context)
    {
        _context = context;
    }

    public async Task<long> AddClipboardContentAsync(ClipboardContent content)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClipboardContents
                (ContentHash, Kind, PlainText, HtmlText, RichText, MarkdownText, JsonText,
                 SvgText, Url, ImagePath, ThumbnailPath, FileReferenceJson, PreviewText,
                 SizeBytes, IsLargeContent, IsSensitive, IsPinned, CreatedAtUtc)
            VALUES
                (@ContentHash, @Kind, @PlainText, @HtmlText, @RichText, @MarkdownText, @JsonText,
                 @SvgText, @Url, @ImagePath, @ThumbnailPath, @FileReferenceJson, @PreviewText,
                 @SizeBytes, @IsLargeContent, @IsSensitive, @IsPinned, @CreatedAtUtc);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@ContentHash", content.ContentHash);
        cmd.Parameters.AddWithValue("@Kind", (int)content.Kind);
        AddNullable(cmd, "@PlainText", content.PlainText);
        AddNullable(cmd, "@HtmlText", content.HtmlText);
        AddNullable(cmd, "@RichText", content.RichText);
        AddNullable(cmd, "@MarkdownText", content.MarkdownText);
        AddNullable(cmd, "@JsonText", content.JsonText);
        AddNullable(cmd, "@SvgText", content.SvgText);
        AddNullable(cmd, "@Url", content.Url);
        AddNullable(cmd, "@ImagePath", content.ImagePath);
        AddNullable(cmd, "@ThumbnailPath", content.ThumbnailPath);
        AddNullable(cmd, "@FileReferenceJson", content.FileReferenceJson);
        AddNullable(cmd, "@PreviewText", content.PreviewText);
        cmd.Parameters.AddWithValue("@SizeBytes", content.SizeBytes);
        cmd.Parameters.AddWithValue("@IsLargeContent", content.IsLargeContent ? 1 : 0);
        cmd.Parameters.AddWithValue("@IsSensitive", content.IsSensitive ? 1 : 0);
        cmd.Parameters.AddWithValue("@IsPinned", content.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", content.CreatedAtUtc.ToString("O"));

        var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
        var newId = Convert.ToInt64(result);
        content.Id = newId;
        return newId;
    }

    public async Task<long> AddClipboardEventAsync(ClipboardEvent evt)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClipboardEvents
                (ClipboardContentId, SourceAppName, SourceProcessName, SourceProcessPath,
                 SourceWindowTitle, SourceKind, CopiedAtUtc, CopyCount, LastCopiedAtUtc)
            VALUES
                (@ClipboardContentId, @SourceAppName, @SourceProcessName, @SourceProcessPath,
                 @SourceWindowTitle, @SourceKind, @CopiedAtUtc, @CopyCount, @LastCopiedAtUtc);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@ClipboardContentId", evt.ClipboardContentId);
        AddNullable(cmd, "@SourceAppName", evt.SourceAppName);
        AddNullable(cmd, "@SourceProcessName", evt.SourceProcessName);
        AddNullable(cmd, "@SourceProcessPath", evt.SourceProcessPath);
        AddNullable(cmd, "@SourceWindowTitle", evt.SourceWindowTitle);
        cmd.Parameters.AddWithValue("@SourceKind", (int)evt.SourceKind);
        cmd.Parameters.AddWithValue("@CopiedAtUtc", evt.CopiedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@CopyCount", evt.CopyCount);
        cmd.Parameters.AddWithValue("@LastCopiedAtUtc", evt.LastCopiedAtUtc.ToString("O"));

        var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
        var newId = Convert.ToInt64(result);
        evt.Id = newId;
        return newId;
    }

    public async Task<List<TimelineItemRecord>> GetRecentTimelineItemsAsync(int limit)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                c.Id, c.ContentHash, c.Kind, c.PlainText, c.HtmlText, c.RichText,
                c.MarkdownText, c.JsonText, c.SvgText, c.Url, c.ImagePath, c.ThumbnailPath,
                c.FileReferenceJson, c.PreviewText, c.SizeBytes, c.IsLargeContent,
                c.IsSensitive, c.IsPinned, c.CreatedAtUtc,
                e.Id, e.ClipboardContentId, e.SourceAppName, e.SourceProcessName,
                e.SourceProcessPath, e.SourceWindowTitle, e.SourceKind,
                e.CopiedAtUtc, e.CopyCount, e.LastCopiedAtUtc
            FROM ClipboardEvents e
            INNER JOIN ClipboardContents c ON c.Id = e.ClipboardContentId
            ORDER BY c.IsPinned DESC, e.CopiedAtUtc DESC
            LIMIT @Limit;
            """;
        cmd.Parameters.AddWithValue("@Limit", limit);

        return await ReadTimelineItemsAsync(cmd).ConfigureAwait(false);
    }

    public async Task<List<TimelineItemRecord>> SearchTimelineItemsAsync(string query, int limit)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                c.Id, c.ContentHash, c.Kind, c.PlainText, c.HtmlText, c.RichText,
                c.MarkdownText, c.JsonText, c.SvgText, c.Url, c.ImagePath, c.ThumbnailPath,
                c.FileReferenceJson, c.PreviewText, c.SizeBytes, c.IsLargeContent,
                c.IsSensitive, c.IsPinned, c.CreatedAtUtc,
                e.Id, e.ClipboardContentId, e.SourceAppName, e.SourceProcessName,
                e.SourceProcessPath, e.SourceWindowTitle, e.SourceKind,
                e.CopiedAtUtc, e.CopyCount, e.LastCopiedAtUtc
            FROM ClipboardEvents e
            INNER JOIN ClipboardContents c ON c.Id = e.ClipboardContentId
            WHERE
                c.PlainText LIKE @Query OR
                c.HtmlText LIKE @Query OR
                c.Url LIKE @Query OR
                c.PreviewText LIKE @Query OR
                e.SourceAppName LIKE @Query
            ORDER BY c.IsPinned DESC, e.CopiedAtUtc DESC
            LIMIT @Limit;
            """;
        cmd.Parameters.AddWithValue("@Query", $"%{query}%");
        cmd.Parameters.AddWithValue("@Limit", limit);

        return await ReadTimelineItemsAsync(cmd).ConfigureAwait(false);
    }

    public async Task<ClipboardContent?> FindContentByHashAsync(string hash)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, ContentHash, Kind, PlainText, HtmlText, RichText, MarkdownText,
                   JsonText, SvgText, Url, ImagePath, ThumbnailPath, FileReferenceJson,
                   PreviewText, SizeBytes, IsLargeContent, IsSensitive, IsPinned, CreatedAtUtc
            FROM ClipboardContents
            WHERE ContentHash = @ContentHash
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@ContentHash", hash);

        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
            return null;

        return ReadContent(reader);
    }

    public async Task<ClipboardEvent?> FindRecentEventForDuplicateAsync(long contentId)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, ClipboardContentId, SourceAppName, SourceProcessName, SourceProcessPath,
                   SourceWindowTitle, SourceKind, CopiedAtUtc, CopyCount, LastCopiedAtUtc
            FROM ClipboardEvents
            WHERE ClipboardContentId = @ContentId
            ORDER BY CopiedAtUtc DESC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@ContentId", contentId);

        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
            return null;

        return ReadEvent(reader);
    }

    public async Task UpdateCopyCountAsync(long eventId, int newCount, DateTime lastCopiedAtUtc)
    {
        using var connection = _context.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE ClipboardEvents
            SET CopyCount = @CopyCount, LastCopiedAtUtc = @LastCopiedAtUtc
            WHERE Id = @Id;
            """;
        cmd.Parameters.AddWithValue("@CopyCount", newCount);
        cmd.Parameters.AddWithValue("@LastCopiedAtUtc", lastCopiedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@Id", eventId);

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task ClearAllAsync()
    {
        using var connection = _context.CreateConnection();

        using var eventsCmd = connection.CreateCommand();
        eventsCmd.CommandText = "DELETE FROM ClipboardEvents;";
        await eventsCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        using var contentCmd = connection.CreateCommand();
        contentCmd.CommandText = "DELETE FROM ClipboardContents;";
        await contentCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<List<TimelineItemRecord>> ReadTimelineItemsAsync(SqliteCommand cmd)
    {
        var results = new List<TimelineItemRecord>();
        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var content = ReadContent(reader, columnOffset: 0);
            var evt = ReadEvent(reader, columnOffset: 19);
            results.Add(new TimelineItemRecord(content, evt));
        }
        return results;
    }

    private static ClipboardContent ReadContent(SqliteDataReader reader, int columnOffset = 0)
    {
        return new ClipboardContent
        {
            Id = reader.GetInt64(columnOffset + 0),
            ContentHash = reader.GetString(columnOffset + 1),
            Kind = (ClipboardItemKind)reader.GetInt32(columnOffset + 2),
            PlainText = GetNullableString(reader, columnOffset + 3),
            HtmlText = GetNullableString(reader, columnOffset + 4),
            RichText = GetNullableString(reader, columnOffset + 5),
            MarkdownText = GetNullableString(reader, columnOffset + 6),
            JsonText = GetNullableString(reader, columnOffset + 7),
            SvgText = GetNullableString(reader, columnOffset + 8),
            Url = GetNullableString(reader, columnOffset + 9),
            ImagePath = GetNullableString(reader, columnOffset + 10),
            ThumbnailPath = GetNullableString(reader, columnOffset + 11),
            FileReferenceJson = GetNullableString(reader, columnOffset + 12),
            PreviewText = GetNullableString(reader, columnOffset + 13),
            SizeBytes = reader.GetInt64(columnOffset + 14),
            IsLargeContent = reader.GetInt32(columnOffset + 15) != 0,
            IsSensitive = reader.GetInt32(columnOffset + 16) != 0,
            IsPinned = reader.GetInt32(columnOffset + 17) != 0,
            CreatedAtUtc = DateTime.Parse(reader.GetString(columnOffset + 18), null, System.Globalization.DateTimeStyles.RoundtripKind),
        };
    }

    private static ClipboardEvent ReadEvent(SqliteDataReader reader, int columnOffset = 0)
    {
        return new ClipboardEvent
        {
            Id = reader.GetInt64(columnOffset + 0),
            ClipboardContentId = reader.GetInt64(columnOffset + 1),
            SourceAppName = GetNullableString(reader, columnOffset + 2),
            SourceProcessName = GetNullableString(reader, columnOffset + 3),
            SourceProcessPath = GetNullableString(reader, columnOffset + 4),
            SourceWindowTitle = GetNullableString(reader, columnOffset + 5),
            SourceKind = (ClipboardSourceKind)reader.GetInt32(columnOffset + 6),
            CopiedAtUtc = DateTime.Parse(reader.GetString(columnOffset + 7), null, System.Globalization.DateTimeStyles.RoundtripKind),
            CopyCount = reader.GetInt32(columnOffset + 8),
            LastCopiedAtUtc = DateTime.Parse(reader.GetString(columnOffset + 9), null, System.Globalization.DateTimeStyles.RoundtripKind),
        };
    }

    private static string? GetNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void AddNullable(SqliteCommand cmd, string name, string? value)
    {
        if (value is null)
            cmd.Parameters.AddWithValue(name, DBNull.Value);
        else
            cmd.Parameters.AddWithValue(name, value);
    }
}
