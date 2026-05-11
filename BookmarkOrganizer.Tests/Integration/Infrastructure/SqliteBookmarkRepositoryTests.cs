using BookmarkOrganizer.Infrastructure;
using BookmarkOrganizer.Models;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace BookmarkOrganizer.Tests.Integration.Infrastructure;

public sealed class SqliteBookmarkRepositoryTests : IAsyncLifetime
{
    private string _dbPath = string.Empty;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bookmark-organizer-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath};Foreign Keys=True";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var initializer = new DatabaseInitializer();
        var migrations = new MigrationRunner(initializer);
        await migrations.ApplyMigrationsAsync(connection);
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    File.Delete(_dbPath);
                    break;
                }
                catch (IOException) when (attempt < 4)
                {
                    Thread.Sleep(50);
                }
            }
        }

        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Foundational")]
    public async Task AddAsync_ThenGetAllAsync_ReturnsBookmark()
    {
        var repository = new SqliteBookmarkRepository(_connectionString);

        await repository.AddAsync(new AddBookmarkRequest("https://example.com", "Example", ["dev"]));

        var all = await repository.GetAllAsync(50, Constants.SortCreatedAt);

        all.Should().HaveCount(1);
        all[0].Url.Should().Be("https://example.com");
        all[0].Tags.Select(t => t.Name).Should().Contain("dev");
    }

    [Fact]
    [Trait("Category", "Foundational")]
    public async Task AddTagAndRemoveTag_UpdateTagFilters()
    {
        var repository = new SqliteBookmarkRepository(_connectionString);
        var created = await repository.AddAsync(new AddBookmarkRequest("https://contoso.com", "Contoso", []));

        await repository.AddTagAsync(created.Id, "tools");
        var tagged = await repository.GetByTagsAsync(["tools"], 50, Constants.SortCreatedAt);

        tagged.Should().HaveCount(1);

        await repository.RemoveTagAsync(created.Id, "tools");
        var afterRemove = await repository.GetByTagsAsync(["tools"], 50, Constants.SortCreatedAt);

        afterRemove.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Foundational")]
    public async Task DeleteAsync_RemovesBookmark()
    {
        var repository = new SqliteBookmarkRepository(_connectionString);
        var created = await repository.AddAsync(new AddBookmarkRequest("https://delete.example", "Delete", []));

        var deleted = await repository.DeleteAsync(created.Id);
        var fetched = await repository.GetByIdAsync(created.Id);

        deleted.Should().BeTrue();
        fetched.Should().BeNull();
    }
}
