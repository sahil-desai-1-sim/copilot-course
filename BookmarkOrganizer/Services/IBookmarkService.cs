using BookmarkOrganizer.Models;

namespace BookmarkOrganizer.Services;

/// <summary>
/// Business logic contract for core bookmark operations.
/// </summary>
public interface IBookmarkService
{
    /// <summary>Adds a bookmark and returns either a newly created or existing duplicate result.</summary>
    Task<AddBookmarkResult> AddAsync(string url, string? title, IReadOnlyList<string> tags, CancellationToken cancellationToken = default);

    /// <summary>Gets bookmarks using optional tag filters, sorting, and limit.</summary>
    Task<IReadOnlyList<Bookmark>> GetAllAsync(IReadOnlyList<string> tags, int limit, string sort, CancellationToken cancellationToken = default);
}
