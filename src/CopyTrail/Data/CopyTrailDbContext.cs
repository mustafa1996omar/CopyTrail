using Microsoft.Data.Sqlite;

namespace CopyTrail.Data;

public sealed class CopyTrailDbContext
{
    public string DbPath { get; }

    public CopyTrailDbContext(string dbPath)
    {
        DbPath = dbPath;
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        return connection;
    }
}
