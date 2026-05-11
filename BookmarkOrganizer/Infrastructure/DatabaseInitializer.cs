using Microsoft.Data.Sqlite;

namespace BookmarkOrganizer.Infrastructure;

/// <summary>
/// Creates base database schema for bookmark storage.
/// </summary>
public sealed class DatabaseInitializer
{
    /// <summary>
    /// Ensures all required schema objects exist.
    /// </summary>
    public async Task EnsureCreatedAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS bookmarks (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                url        TEXT    NOT NULL UNIQUE,
                title      TEXT    NOT NULL,
                created_at TEXT    NOT NULL,
                updated_at TEXT    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS tags (
                id   INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT    NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS bookmark_tags (
                bookmark_id INTEGER NOT NULL REFERENCES bookmarks(id) ON DELETE CASCADE,
                tag_id      INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                PRIMARY KEY (bookmark_id, tag_id)
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS bookmarks_fts USING fts5(
                title,
                url,
                tags,
                tokenize='unicode61'
            );

            CREATE INDEX IF NOT EXISTS idx_bookmarks_created_at ON bookmarks(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_bookmark_tags_tag_id ON bookmark_tags(tag_id);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
