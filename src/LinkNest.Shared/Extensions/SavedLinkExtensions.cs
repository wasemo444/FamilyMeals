using System.Globalization;
using LinkNest.Shared.Models;

namespace LinkNest.Shared.Extensions;

/// <summary>
/// Culture-aware display and search helpers for <see cref="SavedLink"/> instances.
/// </summary>
public static class SavedLinkExtensions
{
    /// <summary>
    /// Returns the best available title for the given UI culture, falling back across languages,
    /// legacy storage, preview metadata, and the raw URL.
    /// </summary>
    /// <param name="link">Link whose titles are evaluated.</param>
    /// <param name="culture">Active UI culture determining primary language preference.</param>
    /// <returns>A non-empty display string suitable for list and card rendering.</returns>
    public static string GetLocalizedTitle(this SavedLink link, CultureInfo culture)
    {
        var isArabic = culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase);
        var localizedTitle = isArabic ? link.TitleAr : link.TitleEn;
        if (!string.IsNullOrWhiteSpace(localizedTitle))
        {
            return localizedTitle;
        }

        var fallbackTitle = isArabic ? link.TitleEn : link.TitleAr;
        if (!string.IsNullOrWhiteSpace(fallbackTitle))
        {
            return fallbackTitle;
        }

        if (!string.IsNullOrWhiteSpace(link.LegacyTitle))
        {
            return link.LegacyTitle;
        }

        return link.PreviewTitle ?? link.Url;
    }

    /// <summary>
    /// Determines whether the link matches a case-insensitive search across titles, notes, and URL.
    /// </summary>
    /// <param name="link">Link to test.</param>
    /// <param name="searchTerm">User-entered filter text; empty or whitespace matches all links.</param>
    /// <returns><see langword="true"/> when the link should appear in filtered results.</returns>
    public static bool MatchesSearch(this SavedLink link, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return true;
        }

        return Contains(link.TitleEn, searchTerm)
            || Contains(link.TitleAr, searchTerm)
            || Contains(link.LegacyTitle, searchTerm)
            || Contains(link.PreviewTitle, searchTerm)
            || Contains(link.Note, searchTerm)
            || Contains(link.Url, searchTerm);
    }

    private static bool Contains(string? value, string searchTerm) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
