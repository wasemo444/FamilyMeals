using LinkNest.Api.Services;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Minimal API routes for fetching Open Graph metadata and preview images for external URLs.
/// </summary>
/// <remarks>
/// Requires authentication. Blocked or unreachable URLs return <c>404 Not Found</c> (SSRF guard via <see cref="Services.LinkPreviewUrlGuard"/>).
/// </remarks>
public static class LinkPreviewEndpoints
{
    /// <summary>
    /// Maps <c>/api/link-preview</c> and <c>/api/link-preview/image</c> endpoints.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapLinkPreviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/link-preview", async (string url, LinkPreviewService service, CancellationToken cancellationToken) =>
        {
            var preview = await service.FetchAsync(url, cancellationToken);
            return preview is null ? Results.NotFound() : Results.Ok(preview);
        })
        .RequireAuthorization();

        endpoints.MapGet("/api/link-preview/image", async (string url, LinkPreviewService service, CancellationToken cancellationToken) =>
        {
            var image = await service.FetchImageAsync(url, cancellationToken);
            return image is null
                ? Results.NotFound()
                : Results.File(image.Value.Bytes, image.Value.ContentType);
        })
        .RequireAuthorization();

        return endpoints;
    }
}
