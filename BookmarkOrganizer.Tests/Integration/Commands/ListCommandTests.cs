using System.Text.Json;
using FluentAssertions;

namespace BookmarkOrganizer.Tests.Integration.Commands;

public sealed class ListCommandTests : IAsyncLifetime
{
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bookmark-list-command-{Guid.NewGuid():N}.db");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        TryDelete(_dbPath);
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task List_GivenNoBookmarks_ShowsEmptyStateMessage()
    {
        var result = await CliTestHost.RunAsync("--db", _dbPath, "list");

        result.ExitCode.Should().Be(0, result.CombinedOutput);
        result.StdOut.Should().Contain("No bookmarks found.");
        result.StdOut.Should().Contain("Tip: Use 'bookmark add <url>'");
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task List_GivenJsonOption_ReturnsJsonArray()
    {
        await SeedAsync();

        var result = await CliTestHost.RunAsync("--db", _dbPath, "list", "--json");

        result.ExitCode.Should().Be(0, result.CombinedOutput);
        using var json = JsonDocument.Parse(result.StdOut);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement.GetArrayLength().Should().Be(3);
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task List_GivenLimitOption_ReturnsLimitedRows()
    {
        await SeedAsync();

        var result = await CliTestHost.RunAsync("--db", _dbPath, "list", "--limit", "1", "--json");

        result.ExitCode.Should().Be(0, result.CombinedOutput);
        using var json = JsonDocument.Parse(result.StdOut);
        json.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task List_GivenSortTitle_ReturnsAlphabeticalOrder()
    {
        await SeedAsync();

        var result = await CliTestHost.RunAsync("--db", _dbPath, "list", "--sort", "title", "--json");

        result.ExitCode.Should().Be(0, result.CombinedOutput);
        using var json = JsonDocument.Parse(result.StdOut);
        var titles = json.RootElement.EnumerateArray().Select(x => x.GetProperty("Title").GetString()).ToArray();
        titles.Should().Equal("Alpha", "Beta", "Gamma");
    }

    private async Task SeedAsync()
    {
        await CliTestHost.RunAsync("--db", _dbPath, "add", "https://seed-1.example", "--title", "Gamma");
        await CliTestHost.RunAsync("--db", _dbPath, "add", "https://seed-2.example", "--title", "Alpha");
        await CliTestHost.RunAsync("--db", _dbPath, "add", "https://seed-3.example", "--title", "Beta");
    }

    private static void TryDelete(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
