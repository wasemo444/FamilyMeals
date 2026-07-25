using ManageFamilyMeals.Api.Services;

namespace ManageFamilyMeals.Api.Endpoints;

public static class LinkPreviewEndpoints
{
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
