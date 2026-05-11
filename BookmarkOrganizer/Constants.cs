namespace BookmarkOrganizer;

/// <summary>
/// Centralized constants for validation, schema, and default behavior.
/// </summary>
public static class Constants
{
    public const string DatabaseFileName = "bookmarks.db";
    public const string AppFolderName = "BookmarkOrganizer";

    public const string BookmarksTable = "bookmarks";
    public const string TagsTable = "tags";
    public const string BookmarkTagsTable = "bookmark_tags";
    public const string BookmarksFtsTable = "bookmarks_fts";

    public const int DefaultLimit = 50;
    public const int MaxUrlLength = 2048;
    public const int MaxTitleLength = 255;
    public const int MaxTagLength = 50;

    public const string TagPattern = "^[a-z0-9][a-z0-9-]{0,49}$";
    public const string SortCreatedAt = "created_at";
    public const string SortUpdatedAt = "updated_at";
    public const string SortTitle = "title";
}
