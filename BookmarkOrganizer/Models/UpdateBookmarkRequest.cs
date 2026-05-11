namespace BookmarkOrganizer.Models;

/// <summary>
/// Input model for updating a bookmark.
/// </summary>
public sealed record UpdateBookmarkRequest(
    long Id,
    string? Title,
    IReadOnlyList<string>? Tags);
