namespace LinkNest.Api.Services;

/// <summary>
/// SSRF-safe HTTP fetcher that validates URLs (including redirect targets) before connecting.
/// </summary>
public interface ISafeUrlFetcher
{
    /// <inheritdoc cref="ISafeUrlValidator.IsAllowedUrlAsync"/>
    Task<bool> IsAllowedUrlAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a GET request when the URL passes validation; follows redirects only to allowed targets.
    /// </summary>
    /// <returns>The response message, or <see langword="null"/> when the URL is blocked or unreachable.</returns>
    Task<HttpResponseMessage?> GetAsync(
        Uri uri,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancellationToken = default);
}
