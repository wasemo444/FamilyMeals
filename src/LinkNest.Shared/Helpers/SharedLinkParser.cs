using System.Text.RegularExpressions;

namespace LinkNest.Shared.Helpers;

/// <summary>
/// Parses URLs and titles from Android/iOS share intents and clipboard payloads
/// when the user saves a link via the Share page.
/// </summary>
public static partial class SharedLinkParser
{
    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    /// <summary>
    /// Resolves an HTTP(S) URL from a direct share URL or by scanning shared text.
    /// </summary>
    /// <param name="sharedUrl">Explicit URL from the share intent, if present.</param>
    /// <param name="sharedText">Free-form shared text that may contain an embedded URL.</param>
    /// <returns>The first valid HTTP(S) URL found, or <see langword="null"/> when none is detected.</returns>
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

    /// <summary>
    /// Derives a display title from an explicit share title or by stripping the URL from shared text.
    /// </summary>
    /// <param name="sharedTitle">Explicit title from the share intent, if present.</param>
    /// <param name="sharedText">Free-form shared text that may contain both title and URL.</param>
    /// <param name="resolvedUrl">Already-extracted URL to remove from <paramref name="sharedText"/>.</param>
    /// <returns>A trimmed title, or <see langword="null"/> when no title can be inferred.</returns>
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
