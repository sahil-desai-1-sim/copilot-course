using Microsoft.Data.Sqlite;

namespace BookmarkOrganizer.Infrastructure;

/// <summary>
/// Applies idempotent schema migrations.
/// </summary>
public sealed class MigrationRunner(DatabaseInitializer initializer)
{
    /// <summary>
    /// Applies pending migrations for the configured database.
    /// </summary>
    public async Task ApplyMigrationsAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        const string migrationTableSql = """
            CREATE TABLE IF NOT EXISTS _migrations (
                version INTEGER PRIMARY KEY,
                applied_at TEXT NOT NULL
            );
            """;

        await using (var setupCommand = connection.CreateCommand())
        {
            setupCommand.CommandText = migrationTableSql;
            await setupCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var hasV1 = await HasMigrationAsync(connection, 1, cancellationToken);
        if (!hasV1)
        {
            await initializer.EnsureCreatedAsync(connection, cancellationToken);
            await RecordMigrationAsync(connection, 1, cancellationToken);
        }
    }

    private static async Task<bool> HasMigrationAsync(SqliteConnection connection, int version, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM _migrations WHERE version = $version;";
        command.Parameters.AddWithValue("$version", version);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task RecordMigrationAsync(SqliteConnection connection, int version, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO _migrations(version, applied_at) VALUES($version, $appliedAt);";
        command.Parameters.AddWithValue("$version", version);
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
