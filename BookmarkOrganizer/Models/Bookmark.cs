namespace BookmarkOrganizer.Models;

/// <summary>
/// Represents a saved web bookmark.
/// </summary>
/// <param name="Id">Database identifier.</param>
/// <param name="Url">Absolute URL for the bookmark.</param>
/// <param name="Title">Display title.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="UpdatedAt">Last update timestamp.</param>
/// <param name="Tags">Associated tags.</param>
public sealed record Bookmark(
    long Id,
    string Url,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<Tag> Tags);
