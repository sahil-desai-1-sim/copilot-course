using System.CommandLine;
using System.Text.Json;
using BookmarkOrganizer.Services;

namespace BookmarkOrganizer.Commands;

/// <summary>
/// Command factory for bookmark list operations.
/// </summary>
public static class ListCommandFactory
{
    public static Command Create(IBookmarkService bookmarkService)
    {
        var tagOption = new Option<string[]>("--tag")
        {
            Description = "Filter by tag (repeatable).",
            AllowMultipleArgumentsPerToken = true
        };
        var limitOption = new Option<int?>("--limit")
        {
            Description = "Maximum number of results. Use 0 for all."
        };
        var sortOption = new Option<string?>("--sort")
        {
            Description = "Sort field: created_at, updated_at, or title."
        };
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output bookmarks as JSON."
        };

        var command = new Command("list", "List saved bookmarks.")
        {
            tagOption,
            limitOption,
            sortOption,
            jsonOption
        };

        command.SetAction(async parseResult =>
        {
            var tags = parseResult.GetValue(tagOption) ?? Array.Empty<string>();
            var limit = parseResult.GetValue(limitOption) ?? Constants.DefaultLimit;
            var sort = parseResult.GetValue(sortOption) ?? Constants.SortCreatedAt;
            var outputJson = parseResult.GetValue(jsonOption);

            try
            {
                var bookmarks = await bookmarkService.GetAllAsync(tags, limit, sort, default);

                if (outputJson)
                {
                    Console.WriteLine(JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                    return 0;
                }

                if (bookmarks.Count == 0)
                {
                    if (tags.Length == 0)
                    {
                        Console.WriteLine("No bookmarks found.");
                        Console.WriteLine("Tip: Use 'bookmark add <url>' to save your first bookmark.");
                    }
                    else
                    {
                        Console.WriteLine($"No bookmarks found matching tag(s): {string.Join(", ", tags)}.");
                    }

                    return 0;
                }

                Console.WriteLine($"{bookmarks.Count} bookmark(s) found.");
                Console.WriteLine();
                Console.WriteLine(" ID  Title                          URL                            Tags            Added");
                Console.WriteLine("---  -----------------------------  -----------------------------  --------------  ------------");

                foreach (var bookmark in bookmarks)
                {
                    var tagsText = bookmark.Tags.Count == 0 ? "(none)" : string.Join(",", bookmark.Tags.Select(t => t.Name));
                    Console.WriteLine($" {bookmark.Id,2}  {Trim(bookmark.Title, 29),-29}  {Trim(bookmark.Url, 29),-29}  {Trim(tagsText, 14),-14}  {bookmark.CreatedAt:yyyy-MM-dd}");
                }

                return 0;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message} Please update your command options and try again.");
                return 1;
            }
        });

        return command;
    }

    private static string Trim(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
    }
}
