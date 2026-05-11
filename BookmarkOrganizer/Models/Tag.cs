namespace BookmarkOrganizer.Models;

/// <summary>
/// Represents a bookmark tag label.
/// </summary>
/// <param name="Id">Database identifier.</param>
/// <param name="Name">Normalized lowercase tag name.</param>
public sealed record Tag(long Id, string Name);
