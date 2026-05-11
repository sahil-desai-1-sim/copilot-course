using BookmarkOrganizer.Models;

namespace BookmarkOrganizer.Services;

/// <summary>
/// Result model for add operations, including duplicate detection outcomes.
/// </summary>
public sealed record AddBookmarkResult(bool IsDuplicate, Bookmark Bookmark);
