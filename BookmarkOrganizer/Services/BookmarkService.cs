using BookmarkOrganizer.Infrastructure;
using BookmarkOrganizer.Models;

namespace BookmarkOrganizer.Services;

/// <summary>
/// Implements business logic for adding and listing bookmarks.
/// </summary>
public sealed class BookmarkService(IBookmarkRepository repository) : IBookmarkService
{
    public async Task<AddBookmarkResult> AddAsync(string url, string? title, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        if (!BookmarkValidation.IsValidUrl(url))
        {
            throw new ArgumentException("URL must be an absolute URL with scheme and host.", nameof(url));
        }

        foreach (var tag in tags)
        {
            if (!BookmarkValidation.IsValidTag(tag))
            {
                throw new ArgumentException($"Tag '{tag}' is invalid. Tags must match {Constants.TagPattern}.", nameof(tags));
            }
        }

        var normalizedUrl = url.Trim();
        var existing = await repository.FindByUrlAsync(normalizedUrl, cancellationToken);
        if (existing is not null)
        {
            return new AddBookmarkResult(true, existing);
        }

        var created = await repository.AddAsync(
            new AddBookmarkRequest(normalizedUrl, title, tags),
            cancellationToken);

        return new AddBookmarkResult(false, created);
    }

    public async Task<IReadOnlyList<Bookmark>> GetAllAsync(IReadOnlyList<string> tags, int limit, string sort, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = limit < 0 ? Constants.DefaultLimit : limit;
        var normalizedSort = NormalizeSort(sort);

        if (tags.Count == 0)
        {
            return await repository.GetAllAsync(normalizedLimit, normalizedSort, cancellationToken);
        }

        foreach (var tag in tags)
        {
            if (!BookmarkValidation.IsValidTag(tag))
            {
                throw new ArgumentException($"Tag '{tag}' is invalid. Tags must match {Constants.TagPattern}.", nameof(tags));
            }
        }

        return await repository.GetByTagsAsync(tags, normalizedLimit, normalizedSort, cancellationToken);
    }

    private static string NormalizeSort(string? sort)
    {
        return sort switch
        {
            Constants.SortUpdatedAt => Constants.SortUpdatedAt,
            Constants.SortTitle => Constants.SortTitle,
            _ => Constants.SortCreatedAt
        };
    }
}
