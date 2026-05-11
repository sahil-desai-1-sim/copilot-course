namespace BookmarkOrganizer.Infrastructure;

/// <summary>
/// Resolves the default and overrideable database path for the bookmark store.
/// </summary>
public static class DataStorePath
{
    /// <summary>
    /// Resolves a filesystem path to the SQLite database.
    /// </summary>
    /// <param name="overridePath">Optional explicit database path.</param>
    /// <returns>Absolute path to the SQLite file.</returns>
    public static string Resolve(string? overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folderPath = Path.Combine(appDataPath, Constants.AppFolderName);
        Directory.CreateDirectory(folderPath);

        return Path.Combine(folderPath, Constants.DatabaseFileName);
    }
}
