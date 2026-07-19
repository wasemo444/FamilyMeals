using System.Text.RegularExpressions;

namespace ManageFamilyMeals.Shared.Helpers;

public static partial class SharedLinkParser
{
    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    public static string? ExtractUrl(string? sharedUrl, string? sharedText)
    {
        if (Uri.TryCreate(sharedUrl, UriKind.Absolute, out var directUri)
            && (directUri.Scheme == Uri.UriSchemeHttp || directUri.Scheme == Uri.UriSchemeHttps))
        {
            return directUri.ToString();
        }

        if (string.IsNullOrWhiteSpace(sharedText))
        {
            return null;
        }

        var match = UrlPattern().Match(sharedText);
        return match.Success ? match.Value.TrimEnd('.', ',', ';') : null;
    }

    public static string? ExtractTitle(string? sharedTitle, string? sharedText, string? resolvedUrl)
    {
        if (!string.IsNullOrWhiteSpace(sharedTitle))
        {
            return sharedTitle.Trim();
        }

        if (string.IsNullOrWhiteSpace(sharedText))
        {
            return null;
        }

        var text = sharedText.Trim();
        if (!string.IsNullOrWhiteSpace(resolvedUrl))
        {
            text = text.Replace(resolvedUrl, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
