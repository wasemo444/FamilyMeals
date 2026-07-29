namespace LinkNest.Api.Services;

/// <summary>
/// Validates whether a URI is safe for server-side HTTP fetching (SSRF mitigation).
/// </summary>
public interface ISafeUrlValidator
{
    /// <summary>
    /// Determines whether a URI is safe to fetch (HTTP/HTTPS only, public addresses after DNS resolution).
    /// </summary>
    Task<bool> IsAllowedUrlAsync(Uri uri, CancellationToken cancellationToken = default);
}
