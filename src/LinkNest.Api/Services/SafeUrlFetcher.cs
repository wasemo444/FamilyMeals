namespace LinkNest.Api.Services;

/// <summary>
/// Fetches HTTP(S) resources only when the target URL and any redirect targets pass SSRF validation.
/// </summary>
public sealed class SafeUrlFetcher(
    IHttpClientFactory httpClientFactory,
    ISafeUrlValidator urlValidator,
    ILogger<SafeUrlFetcher> logger) : ISafeUrlFetcher
{
    /// <inheritdoc />
    public Task<bool> IsAllowedUrlAsync(Uri uri, CancellationToken cancellationToken = default) =>
        urlValidator.IsAllowedUrlAsync(uri, cancellationToken);

    /// <inheritdoc />
    public async Task<HttpResponseMessage?> GetAsync(
        Uri uri,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancellationToken = default)
    {
        if (!SafeUrlValidator.IsAllowedScheme(uri)
            || !await urlValidator.IsAllowedUrlAsync(uri, cancellationToken))
        {
            logger.LogWarning("Blocked outbound fetch for non-public URL {Url}", uri);
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(SafeUrlFetcher));
            return await client.GetAsync(uri, completionOption, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed outbound fetch for {Url}", uri);
            return null;
        }
    }
}
