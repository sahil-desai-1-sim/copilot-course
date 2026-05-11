using System.Text.RegularExpressions;

namespace BookmarkOrganizer.Models;

/// <summary>
/// Validation helpers for bookmark inputs.
/// </summary>
public static partial class BookmarkValidation
{
    /// <summary>
    /// Returns true when the supplied URL is an absolute URI with a host.
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > Constants.MaxUrlLength)
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed) &&
               !string.IsNullOrWhiteSpace(parsed.Host);
    }

    /// <summary>
    /// Returns true when the supplied tag follows the configured tag naming rule.
    /// </summary>
    public static bool IsValidTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag.Length > Constants.MaxTagLength)
        {
            return false;
        }

        return TagRegex().IsMatch(tag.Trim().ToLowerInvariant());
    }

    [GeneratedRegex(Constants.TagPattern)]
    private static partial Regex TagRegex();
}
