using BookmarkOrganizer.Models;

namespace BookmarkOrganizer.Infrastructure;

/// <summary>
/// Data access contract for bookmark persistence and query operations.
/// </summary>
public interface IBookmarkRepository
{
    /// <summary>Adds a new bookmark.</summary>
    Task<Bookmark> AddAsync(AddBookmarkRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a bookmark by URL if it exists.</summary>
    Task<Bookmark?> FindByUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Returns all bookmarks, sorted and optionally limited.</summary>
    Task<IReadOnlyList<Bookmark>> GetAllAsync(int limit, string sort, CancellationToken cancellationToken = default);

    /// <summary>Returns bookmarks that contain all provided tags.</summary>
    Task<IReadOnlyList<Bookmark>> GetByTagsAsync(IReadOnlyList<string> tags, int limit, string sort, CancellationToken cancellationToken = default);

    /// <summary>Returns a bookmark by ID if it exists.</summary>
    Task<Bookmark?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Searches bookmarks by keyword across title, URL, and tags.</summary>
    Task<IReadOnlyList<Bookmark>> SearchAsync(string keyword, int limit, CancellationToken cancellationToken = default);

    /// <summary>Updates title and optionally tags of a bookmark.</summary>
    Task<Bookmark> UpdateAsync(UpdateBookmarkRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a bookmark by ID.</summary>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Adds a tag to a bookmark.</summary>
    Task<bool> AddTagAsync(long id, string tag, CancellationToken cancellationToken = default);

    /// <summary>Removes a tag from a bookmark.</summary>
    Task<bool> RemoveTagAsync(long id, string tag, CancellationToken cancellationToken = default);
}
