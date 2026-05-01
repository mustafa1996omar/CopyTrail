using System.IO;
using Microsoft.Data.Sqlite;

namespace CopyTrail.Data;

public static class DatabaseInitializer
{
    private const string CreateClipboardContentsTable = """
        CREATE TABLE IF NOT EXISTS ClipboardContents (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ContentHash TEXT NOT NULL,
            Kind INTEGER NOT NULL,
            PlainText TEXT,
            HtmlText TEXT,
            RichText TEXT,
            MarkdownText TEXT,
            JsonText TEXT,
            SvgText TEXT,
            Url TEXT,
            ImagePath TEXT,
            ThumbnailPath TEXT,
            FileReferenceJson TEXT,
            PreviewText TEXT,
            SizeBytes INTEGER NOT NULL DEFAULT 0,
            IsLargeContent INTEGER NOT NULL DEFAULT 0,
            IsSensitive INTEGER NOT NULL DEFAULT 0,
            IsPinned INTEGER NOT NULL DEFAULT 0,
            CreatedAtUtc TEXT NOT NULL
        );
        """;

    private const string CreateClipboardEventsTable = """
        CREATE TABLE IF NOT EXISTS ClipboardEvents (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ClipboardContentId INTEGER NOT NULL,
            SourceAppName TEXT,
            SourceProcessName TEXT,
            SourceProcessPath TEXT,
            SourceWindowTitle TEXT,
            SourceKind INTEGER NOT NULL DEFAULT 0,
            CopiedAtUtc TEXT NOT NULL,
            CopyCount INTEGER NOT NULL DEFAULT 1,
            LastCopiedAtUtc TEXT NOT NULL,
            FOREIGN KEY (ClipboardContentId) REFERENCES ClipboardContents(Id) ON DELETE CASCADE
        );
        """;

    private static readonly string[] CreateIndexStatements =
    [
        "CREATE INDEX IF NOT EXISTS idx_content_hash ON ClipboardContents(ContentHash);",
        "CREATE INDEX IF NOT EXISTS idx_content_kind ON ClipboardContents(Kind);",
        "CREATE INDEX IF NOT EXISTS idx_event_copied_at ON ClipboardEvents(CopiedAtUtc DESC);",
        "CREATE INDEX IF NOT EXISTS idx_event_source_app ON ClipboardEvents(SourceAppName);",
        "CREATE INDEX IF NOT EXISTS idx_event_content_id ON ClipboardEvents(ClipboardContentId);",
    ];

    public static void Initialize(CopyTrailDbContext context)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(context.DbPath)!);

        using var connection = context.CreateConnection();

        Execute(connection, "PRAGMA foreign_keys = ON;");
        Execute(connection, "PRAGMA journal_mode = WAL;");
        Execute(connection, CreateClipboardContentsTable);
        Execute(connection, CreateClipboardEventsTable);

        foreach (var index in CreateIndexStatements)
            Execute(connection, index);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
