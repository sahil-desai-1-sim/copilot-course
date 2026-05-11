namespace BookmarkOrganizer.Models;

/// <summary>
/// Input model for creating a bookmark.
/// </summary>
public sealed record AddBookmarkRequest(
    string Url,
    string? Title,
    IReadOnlyList<string> Tags);
