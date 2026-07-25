namespace ManageFamilyMeals.Shared.Auth;

/// <summary>
/// Helpers for safe post-login and post-logout navigation within the Blazor app.
/// </summary>
public static class AuthNavigation
{
    /// <summary>
    /// Returns a local relative return URL, rejecting open redirects and protocol-relative paths.
    /// </summary>
    /// <param name="returnUrl">Requested return path from the query string or form.</param>
    /// <param name="fallback">Path used when <paramref name="returnUrl"/> is missing or unsafe.</param>
    /// <returns>A safe relative path starting with <c>/</c>.</returns>
    /// <remarks>
    /// Rejects URLs that start with <c>//</c> (protocol-relative) or contain backslashes,
    /// which could enable open-redirect attacks.
    /// </remarks>
    public static string GetSafeReturnUrl(string? returnUrl, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return fallback;
        }

        if (!returnUrl.StartsWith('/') || returnUrl.StartsWith("//", StringComparison.Ordinal) || returnUrl.Contains('\\'))
        {
            return fallback;
        }

        return returnUrl;
    }
}
