using System.Text.Json;
using FluentAssertions;

namespace BookmarkOrganizer.Tests.Integration.Commands;

public sealed class AddCommandTests : IAsyncLifetime
{
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bookmark-add-command-{Guid.NewGuid():N}.db");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        TryDelete(_dbPath);
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task Add_GivenValidInput_ReturnsSuccessAndSavesBookmark()
    {
        var result = await CliTestHost.RunAsync("--db", _dbPath, "add", "https://example.com", "--title", "Example", "--tag", "dev");

        result.ExitCode.Should().Be(0, result.CombinedOutput);
        result.StdOut.Should().Contain("Bookmark saved.");
        result.StdOut.Should().Contain("https://example.com");

        var list = await CliTestHost.RunAsync("--db", _dbPath, "list", "--json");
        list.ExitCode.Should().Be(0, list.CombinedOutput);

        using var json = JsonDocument.Parse(list.StdOut);
        json.RootElement.GetArrayLength().Should().Be(1);
        json.RootElement[0].GetProperty("Url").GetString().Should().Be("https://example.com");
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task Add_GivenDuplicateUrl_ReturnsExitCode2()
    {
        var first = await CliTestHost.RunAsync("--db", _dbPath, "add", "https://dup.example", "--title", "First");
        first.ExitCode.Should().Be(0, first.CombinedOutput);

        var second = await CliTestHost.RunAsync("--db", _dbPath, "add", "https://dup.example", "--title", "Second");

        second.ExitCode.Should().Be(2, second.CombinedOutput);
        second.StdOut.Should().Contain("Warning: This URL is already saved");

        var list = await CliTestHost.RunAsync("--db", _dbPath, "list", "--json");
        using var json = JsonDocument.Parse(list.StdOut);
        json.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task Add_GivenMalformedUrl_ReturnsExitCode1()
    {
        var result = await CliTestHost.RunAsync("--db", _dbPath, "add", "not-a-url");

        result.ExitCode.Should().Be(1, result.CombinedOutput);
        result.StdErr.Should().Contain("Error:");
    }

    [Fact]
    [Trait("Category", "US1")]
    public async Task Add_GivenJsonOption_PrintsJsonPayload()
    {
        var result = await CliTestHost.RunAsync("--db", _dbPath, "add", "https://json.example", "--title", "Json Title", "--json");

        result.ExitCode.Should().Be(0, result.CombinedOutput);

        using var json = JsonDocument.Parse(result.StdOut);
        json.RootElement.GetProperty("Url").GetString().Should().Be("https://json.example");
        json.RootElement.GetProperty("Title").GetString().Should().Be("Json Title");
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
