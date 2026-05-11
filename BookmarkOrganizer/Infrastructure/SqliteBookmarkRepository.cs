using System.Text;
using BookmarkOrganizer.Models;
using Microsoft.Data.Sqlite;

namespace BookmarkOrganizer.Infrastructure;

/// <summary>
/// SQLite-backed bookmark repository implementation.
/// </summary>
public sealed class SqliteBookmarkRepository(string connectionString) : IBookmarkRepository
{
    public async Task<Bookmark> AddAsync(AddBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var title = string.IsNullOrWhiteSpace(request.Title) ? request.Url : request.Title.Trim();

        var insertBookmarkSql = """
            INSERT INTO bookmarks(url, title, created_at, updated_at)
            VALUES($url, $title, $createdAt, $updatedAt);
            SELECT last_insert_rowid();
            """;

        long bookmarkId;
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = insertBookmarkSql;
            command.Parameters.AddWithValue("$url", request.Url.Trim());
            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$createdAt", now.ToString("O"));
            command.Parameters.AddWithValue("$updatedAt", now.ToString("O"));
            var result = await command.ExecuteScalarAsync(cancellationToken);
            bookmarkId = (long)(result ?? 0L);
        }

        foreach (var tag in NormalizeTags(request.Tags))
        {
            await AddTagInternalAsync(connection, transaction, bookmarkId, tag, cancellationToken);
        }

        await SyncFtsAsync(connection, transaction, bookmarkId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(bookmarkId, cancellationToken))!;
    }

    public async Task<Bookmark?> FindByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM bookmarks WHERE url = $url LIMIT 1;";
        command.Parameters.AddWithValue("$url", url.Trim());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            return null;
        }

        return await GetByIdAsync(Convert.ToInt64(result), cancellationToken);
    }

    public async Task<IReadOnlyList<Bookmark>> GetAllAsync(int limit, string sort, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var ids = await QueryIdsAsync(connection, null, limit, sort, cancellationToken);
        return await LoadBookmarksByIdsAsync(connection, ids, cancellationToken);
    }

    public async Task<IReadOnlyList<Bookmark>> GetByTagsAsync(IReadOnlyList<string> tags, int limit, string sort, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var normalizedTags = NormalizeTags(tags).ToArray();
        if (normalizedTags.Length == 0)
        {
            return await GetAllAsync(limit, sort, cancellationToken);
        }

        var sql = new StringBuilder();
        sql.Append("SELECT b.id FROM bookmarks b ");
        sql.Append("JOIN bookmark_tags bt ON bt.bookmark_id = b.id ");
        sql.Append("JOIN tags t ON t.id = bt.tag_id ");
        sql.Append("WHERE t.name IN (");
        for (var i = 0; i < normalizedTags.Length; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }

            sql.Append("$tag" + i);
        }

        sql.Append(") GROUP BY b.id HAVING COUNT(DISTINCT t.name) = $tagCount ");
        sql.Append("ORDER BY " + BuildSort(sort) + " ");
        if (limit > 0)
        {
            sql.Append("LIMIT $limit");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        command.Parameters.AddWithValue("$tagCount", normalizedTags.Length);
        if (limit > 0)
        {
            command.Parameters.AddWithValue("$limit", limit);
        }

        for (var i = 0; i < normalizedTags.Length; i++)
        {
            command.Parameters.AddWithValue("$tag" + i, normalizedTags[i]);
        }

        var ids = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt64(0));
        }

        return await LoadBookmarksByIdsAsync(connection, ids, cancellationToken);
    }

    public async Task<Bookmark?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await GetByIdInternalAsync(connection, null, id, cancellationToken);
    }

    public async Task<IReadOnlyList<Bookmark>> SearchAsync(string keyword, int limit, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT b.id
            FROM bookmarks_fts f
            JOIN bookmarks b ON b.id = f.rowid
            WHERE bookmarks_fts MATCH $keyword
            ORDER BY rank
            LIMIT $limit;
            """;

        command.Parameters.AddWithValue("$keyword", keyword + "*");
        command.Parameters.AddWithValue("$limit", limit <= 0 ? Constants.DefaultLimit : limit);

        var ids = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt64(0));
        }

        return await LoadBookmarksByIdsAsync(connection, ids, cancellationToken);
    }

    public async Task<Bookmark> UpdateAsync(UpdateBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var existing = await GetByIdInternalAsync(connection, null, request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Bookmark with id {request.Id} not found.");

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var newTitle = request.Title is null
            ? existing.Title
            : (string.IsNullOrWhiteSpace(request.Title) ? existing.Url : request.Title.Trim());

        await using (var updateBookmarkCommand = connection.CreateCommand())
        {
            updateBookmarkCommand.Transaction = transaction;
            updateBookmarkCommand.CommandText = """
                UPDATE bookmarks
                SET title = $title,
                    updated_at = $updatedAt
                WHERE id = $id;
                """;
            updateBookmarkCommand.Parameters.AddWithValue("$title", newTitle);
            updateBookmarkCommand.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
            updateBookmarkCommand.Parameters.AddWithValue("$id", request.Id);
            await updateBookmarkCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (request.Tags is not null)
        {
            await using var clearTagsCommand = connection.CreateCommand();
            clearTagsCommand.Transaction = transaction;
            clearTagsCommand.CommandText = "DELETE FROM bookmark_tags WHERE bookmark_id = $id;";
            clearTagsCommand.Parameters.AddWithValue("$id", request.Id);
            await clearTagsCommand.ExecuteNonQueryAsync(cancellationToken);

            foreach (var tag in NormalizeTags(request.Tags))
            {
                await AddTagInternalAsync(connection, transaction, request.Id, tag, cancellationToken);
            }
        }

        await SyncFtsAsync(connection, transaction, request.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(request.Id, cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var deleteFtsCommand = connection.CreateCommand())
        {
            deleteFtsCommand.Transaction = transaction;
            deleteFtsCommand.CommandText = "DELETE FROM bookmarks_fts WHERE rowid = $id;";
            deleteFtsCommand.Parameters.AddWithValue("$id", id);
            await deleteFtsCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var deleteCommand = connection.CreateCommand();
        deleteCommand.Transaction = transaction;
        deleteCommand.CommandText = "DELETE FROM bookmarks WHERE id = $id;";
        deleteCommand.Parameters.AddWithValue("$id", id);
        var affected = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> AddTagAsync(long id, string tag, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var exists = await GetByIdInternalAsync(connection, null, id, cancellationToken) is not null;
        if (!exists)
        {
            return false;
        }

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await AddTagInternalAsync(connection, transaction, id, tag, cancellationToken);
        await TouchBookmarkAsync(connection, transaction, id, cancellationToken);
        await SyncFtsAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RemoveTagAsync(long id, string tag, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var exists = await GetByIdInternalAsync(connection, null, id, cancellationToken) is not null;
        if (!exists)
        {
            return false;
        }

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                DELETE FROM bookmark_tags
                WHERE bookmark_id = $bookmarkId
                  AND tag_id = (SELECT id FROM tags WHERE name = $tag);
                """;
            command.Parameters.AddWithValue("$bookmarkId", id);
            command.Parameters.AddWithValue("$tag", tag.Trim().ToLowerInvariant());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await TouchBookmarkAsync(connection, transaction, id, cancellationToken);
        await SyncFtsAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private SqliteConnection CreateConnection() => new(connectionString);

    private static async Task<IReadOnlyList<long>> QueryIdsAsync(SqliteConnection connection, SqliteTransaction? transaction, int limit, string sort, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT id FROM bookmarks ORDER BY {BuildSort(sort)}" + (limit > 0 ? " LIMIT $limit" : string.Empty);
        if (limit > 0)
        {
            command.Parameters.AddWithValue("$limit", limit);
        }

        var ids = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt64(0));
        }

        return ids;
    }

    private static async Task<IReadOnlyList<Bookmark>> LoadBookmarksByIdsAsync(SqliteConnection connection, IEnumerable<long> ids, CancellationToken cancellationToken)
    {
        var list = new List<Bookmark>();
        foreach (var id in ids)
        {
            var bookmark = await GetByIdInternalAsync(connection, null, id, cancellationToken);
            if (bookmark is not null)
            {
                list.Add(bookmark);
            }
        }

        return list;
    }

    private static async Task<Bookmark?> GetByIdInternalAsync(SqliteConnection connection, SqliteTransaction? transaction, long id, CancellationToken cancellationToken)
    {
        await using var bookmarkCommand = connection.CreateCommand();
        bookmarkCommand.Transaction = transaction;
        bookmarkCommand.CommandText = "SELECT id, url, title, created_at, updated_at FROM bookmarks WHERE id = $id LIMIT 1;";
        bookmarkCommand.Parameters.AddWithValue("$id", id);

        await using var bookmarkReader = await bookmarkCommand.ExecuteReaderAsync(cancellationToken);
        if (!await bookmarkReader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var bookmarkId = bookmarkReader.GetInt64(0);
        var url = bookmarkReader.GetString(1);
        var title = bookmarkReader.GetString(2);
        var createdAt = DateTimeOffset.Parse(bookmarkReader.GetString(3));
        var updatedAt = DateTimeOffset.Parse(bookmarkReader.GetString(4));

        var tags = new List<Tag>();
        await using var tagCommand = connection.CreateCommand();
        tagCommand.Transaction = transaction;
        tagCommand.CommandText = """
            SELECT t.id, t.name
            FROM tags t
            JOIN bookmark_tags bt ON bt.tag_id = t.id
            WHERE bt.bookmark_id = $bookmarkId
            ORDER BY t.name;
            """;
        tagCommand.Parameters.AddWithValue("$bookmarkId", bookmarkId);

        await using var tagReader = await tagCommand.ExecuteReaderAsync(cancellationToken);
        while (await tagReader.ReadAsync(cancellationToken))
        {
            tags.Add(new Tag(tagReader.GetInt64(0), tagReader.GetString(1)));
        }

        return new Bookmark(bookmarkId, url, title, createdAt, updatedAt, tags);
    }

    private static async Task AddTagInternalAsync(SqliteConnection connection, SqliteTransaction transaction, long bookmarkId, string tag, CancellationToken cancellationToken)
    {
        var normalizedTag = tag.Trim().ToLowerInvariant();

        await using (var createTagCommand = connection.CreateCommand())
        {
            createTagCommand.Transaction = transaction;
            createTagCommand.CommandText = "INSERT OR IGNORE INTO tags(name) VALUES($name);";
            createTagCommand.Parameters.AddWithValue("$name", normalizedTag);
            await createTagCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var linkCommand = connection.CreateCommand();
        linkCommand.Transaction = transaction;
        linkCommand.CommandText = """
            INSERT OR IGNORE INTO bookmark_tags(bookmark_id, tag_id)
            VALUES($bookmarkId, (SELECT id FROM tags WHERE name = $name));
            """;
        linkCommand.Parameters.AddWithValue("$bookmarkId", bookmarkId);
        linkCommand.Parameters.AddWithValue("$name", normalizedTag);
        await linkCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task TouchBookmarkAsync(SqliteConnection connection, SqliteTransaction transaction, long id, CancellationToken cancellationToken)
    {
        await using var touchCommand = connection.CreateCommand();
        touchCommand.Transaction = transaction;
        touchCommand.CommandText = "UPDATE bookmarks SET updated_at = $updatedAt WHERE id = $id;";
        touchCommand.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        touchCommand.Parameters.AddWithValue("$id", id);
        await touchCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SyncFtsAsync(SqliteConnection connection, SqliteTransaction transaction, long bookmarkId, CancellationToken cancellationToken)
    {
        await using var bookmarkCommand = connection.CreateCommand();
        bookmarkCommand.Transaction = transaction;
        bookmarkCommand.CommandText = "SELECT title, url FROM bookmarks WHERE id = $id;";
        bookmarkCommand.Parameters.AddWithValue("$id", bookmarkId);

        await using var bookmarkReader = await bookmarkCommand.ExecuteReaderAsync(cancellationToken);
        if (!await bookmarkReader.ReadAsync(cancellationToken))
        {
            return;
        }

        var title = bookmarkReader.GetString(0);
        var url = bookmarkReader.GetString(1);

        var tags = new List<string>();
        await using (var tagsCommand = connection.CreateCommand())
        {
            tagsCommand.Transaction = transaction;
            tagsCommand.CommandText = """
                SELECT t.name
                FROM tags t
                JOIN bookmark_tags bt ON bt.tag_id = t.id
                WHERE bt.bookmark_id = $id
                ORDER BY t.name;
                """;
            tagsCommand.Parameters.AddWithValue("$id", bookmarkId);

            await using var tagReader = await tagsCommand.ExecuteReaderAsync(cancellationToken);
            while (await tagReader.ReadAsync(cancellationToken))
            {
                tags.Add(tagReader.GetString(0));
            }
        }

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM bookmarks_fts WHERE rowid = $id;";
            deleteCommand.Parameters.AddWithValue("$id", bookmarkId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText = "INSERT INTO bookmarks_fts(rowid, title, url, tags) VALUES($id, $title, $url, $tags);";
        insertCommand.Parameters.AddWithValue("$id", bookmarkId);
        insertCommand.Parameters.AddWithValue("$title", title);
        insertCommand.Parameters.AddWithValue("$url", url);
        insertCommand.Parameters.AddWithValue("$tags", string.Join(' ', tags));
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IEnumerable<string> NormalizeTags(IReadOnlyList<string> tags)
    {
        return tags
            .Where(static t => !string.IsNullOrWhiteSpace(t))
            .Select(static t => t.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static t => t, StringComparer.Ordinal);
    }

    private static string BuildSort(string sort)
    {
        return sort switch
        {
            Constants.SortUpdatedAt => "updated_at DESC",
            Constants.SortTitle => "title ASC",
            _ => "created_at DESC"
        };
    }
}
