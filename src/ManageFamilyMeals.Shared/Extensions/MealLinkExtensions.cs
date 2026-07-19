using System.Globalization;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Extensions;

public static class MealLinkExtensions
{
    public static string GetLocalizedTitle(this MealLink link, CultureInfo culture)
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

    public static bool MatchesSearch(this MealLink link, string? searchTerm)
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
