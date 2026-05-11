using System.CommandLine;
using System.Text.Json;
using BookmarkOrganizer.Models;
using BookmarkOrganizer.Services;

namespace BookmarkOrganizer.Commands;

/// <summary>
/// Command factory for bookmark add operations.
/// </summary>
public static class AddCommandFactory
{
    public static Command Create(IBookmarkService bookmarkService)
    {
        var urlArgument = new Argument<string>("url")
        {
            Description = "Bookmark URL to save."
        };
        var titleOption = new Option<string?>("--title")
        {
            Description = "Optional title for the bookmark."
        };
        var tagOption = new Option<string[]>("--tag")
        {
            Description = "Tag to attach (repeatable).",
            AllowMultipleArgumentsPerToken = true
        };
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output the saved bookmark as JSON."
        };

        var command = new Command("add", "Save a new bookmark.")
        {
            urlArgument,
            titleOption,
            tagOption,
            jsonOption
        };

        command.SetAction(async parseResult =>
        {
            var cancellationToken = parseResult.InvocationConfiguration.ProcessTerminationTimeout == TimeSpan.Zero
                ? CancellationToken.None
                : default;

            var url = parseResult.GetValue(urlArgument)!;
            var title = parseResult.GetValue(titleOption);
            var tags = parseResult.GetValue(tagOption) ?? Array.Empty<string>();
            var outputJson = parseResult.GetValue(jsonOption);

            try
            {
                var result = await bookmarkService.AddAsync(url, title, tags, cancellationToken);
                if (result.IsDuplicate)
                {
                    WriteDuplicate(result.Bookmark);
                    return 2;
                }

                if (outputJson)
                {
                    Console.WriteLine(JsonSerializer.Serialize(result.Bookmark, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                }
                else
                {
                    WriteSaved(result.Bookmark);
                }

                return 0;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message} Please fix the input and try again.");
                return 1;
            }
        });

        return command;
    }

    private static void WriteSaved(Bookmark bookmark)
    {
        Console.WriteLine("Bookmark saved.");
        Console.WriteLine($"  ID   : {bookmark.Id}");
        Console.WriteLine($"  URL  : {bookmark.Url}");
        Console.WriteLine($"  Title: {bookmark.Title}");
        Console.WriteLine($"  Tags : {(bookmark.Tags.Count == 0 ? "(none)" : string.Join(", ", bookmark.Tags.Select(t => t.Name)))}");
        Console.WriteLine($"  Added: {bookmark.CreatedAt:yyyy-MM-dd HH:mm} UTC");
    }

    private static void WriteDuplicate(Bookmark bookmark)
    {
        Console.WriteLine($"Warning: This URL is already saved (ID {bookmark.Id}).");
        Console.WriteLine($"  ID   : {bookmark.Id}");
        Console.WriteLine($"  URL  : {bookmark.Url}");
        Console.WriteLine($"  Title: {bookmark.Title}");
        Console.WriteLine($"  Tags : {(bookmark.Tags.Count == 0 ? "(none)" : string.Join(", ", bookmark.Tags.Select(t => t.Name)))}");
        Console.WriteLine("No new bookmark was created. Use 'bookmark update <id>' to modify it.");
    }
}
