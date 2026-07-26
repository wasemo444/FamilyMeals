using LinkNest.Shared.Models;
using LinkNest.Shared.Services;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Minimal API routes for meal links within categories, including archive, restore, favorite, and preview updates.
/// </summary>
/// <remarks>
/// All routes require authentication. Ownership violations return <c>404 Not Found</c> (not <c>403</c>).
/// Missing categories when creating a link return <c>404</c> with an error payload.
/// </remarks>
public static class LinkEndpoints
{
    /// <summary>
    /// Maps <c>/api/categories/{categoryId}/links</c> and <c>/api/links</c> endpoints.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var categoryLinks = endpoints.MapGroup("/api/categories/{categoryId:guid}/links").RequireAuthorization();
        var links = endpoints.MapGroup("/api/links").RequireAuthorization();

        categoryLinks.MapGet("/", (Guid categoryId, IContentDataService dataService) =>
            Results.Ok(dataService.GetActiveLinks(categoryId)));

        categoryLinks.MapGet("/favorites", (Guid categoryId, IContentDataService dataService) =>
            Results.Ok(dataService.GetFavoriteLinks(categoryId)));

        categoryLinks.MapGet("/archived", (Guid categoryId, IContentDataService dataService) =>
            Results.Ok(dataService.GetArchivedLinks(categoryId)));

        links.MapGet("/archived", (IContentDataService dataService) =>
            Results.Ok(dataService.GetAllArchivedLinks()));

        categoryLinks.MapPost("/", async (
            Guid categoryId,
            CreateLinkRequest request,
            IContentDataService dataService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return Results.BadRequest(new { error = "Url is required." });
            }

            if (dataService.GetCategory(categoryId) is null)
            {
                return Results.NotFound(new { error = "Category not found." });
            }

            try
            {
                var link = await dataService.AddLinkAsync(
                    categoryId,
                    request.TitleEn ?? string.Empty,
                    request.TitleAr ?? string.Empty,
                    request.Url,
                    request.Note,
                    cancellationToken);

                return Results.Created($"/api/links/{link.Id}", link);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        links.MapPost("/{id:guid}/archive", async (Guid id, IContentDataService dataService, CancellationToken cancellationToken) =>
        {
            try
            {
                var archived = await dataService.ArchiveLinkAsync(id, cancellationToken);
                return archived ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        links.MapPost("/{id:guid}/restore", async (Guid id, IContentDataService dataService, CancellationToken cancellationToken) =>
        {
            try
            {
                var restored = await dataService.RestoreLinkAsync(id, cancellationToken);
                return restored ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        links.MapPost("/{id:guid}/favorite", async (Guid id, IContentDataService dataService, CancellationToken cancellationToken) =>
        {
            try
            {
                await dataService.ToggleLinkFavoriteAsync(id, cancellationToken);
                var link = dataService.GetLink(id);
                return link is null ? Results.NotFound() : Results.Ok(link);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        links.MapPut("/{id:guid}/preview", async (
            Guid id,
            LinkPreviewData preview,
            IContentDataService dataService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await dataService.UpdateLinkPreviewAsync(id, preview, cancellationToken);
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        return endpoints;
    }

    private sealed record CreateLinkRequest(string? TitleEn, string? TitleAr, string Url, string? Note);
}
