using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// Client for fetching Open Graph or HTML preview metadata for a URL before saving a link.
/// </summary>
public interface ILinkPreviewClient
{
    /// <summary>
    /// Retrieves preview metadata for the given URL.
    /// </summary>
    /// <param name="url">Absolute HTTP(S) URL to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed preview data, or <see langword="null"/> when preview extraction fails.</returns>
    Task<LinkPreviewData?> FetchAsync(string url, CancellationToken cancellationToken = default);
}
