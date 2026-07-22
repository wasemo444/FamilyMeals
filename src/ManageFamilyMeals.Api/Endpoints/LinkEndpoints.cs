using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Api.Endpoints;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var categoryLinks = endpoints.MapGroup("/api/categories/{categoryId:guid}/links").RequireAuthorization();
        var links = endpoints.MapGroup("/api/links").RequireAuthorization();

        categoryLinks.MapGet("/", (Guid categoryId, IMealDataService dataService) =>
            Results.Ok(dataService.GetActiveLinks(categoryId)));

        categoryLinks.MapGet("/favorites", (Guid categoryId, IMealDataService dataService) =>
            Results.Ok(dataService.GetFavoriteLinks(categoryId)));

        categoryLinks.MapGet("/archived", (Guid categoryId, IMealDataService dataService) =>
            Results.Ok(dataService.GetArchivedLinks(categoryId)));

        links.MapGet("/archived", (IMealDataService dataService) =>
            Results.Ok(dataService.GetAllArchivedLinks()));

        categoryLinks.MapPost("/", async (
            Guid categoryId,
            CreateLinkRequest request,
            IMealDataService dataService,
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

            var link = await dataService.AddLinkAsync(
                categoryId,
                request.TitleEn ?? string.Empty,
                request.TitleAr ?? string.Empty,
                request.Url,
                request.Note,
                cancellationToken);

            return Results.Created($"/api/links/{link.Id}", link);
        });

        links.MapPost("/{id:guid}/archive", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            var archived = await dataService.ArchiveLinkAsync(id, cancellationToken);
            return archived ? Results.NoContent() : Results.NotFound();
        });

        links.MapPost("/{id:guid}/restore", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            var restored = await dataService.RestoreLinkAsync(id, cancellationToken);
            return restored ? Results.NoContent() : Results.NotFound();
        });

        links.MapPost("/{id:guid}/favorite", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            await dataService.ToggleLinkFavoriteAsync(id, cancellationToken);
            var link = dataService.GetLink(id);
            return link is null ? Results.NotFound() : Results.Ok(link);
        });

        links.MapPut("/{id:guid}/preview", async (
            Guid id,
            LinkPreviewData preview,
            IMealDataService dataService,
            CancellationToken cancellationToken) =>
        {
            await dataService.UpdateLinkPreviewAsync(id, preview, cancellationToken);
            return Results.NoContent();
        });

        return endpoints;
    }

    private sealed record CreateLinkRequest(string? TitleEn, string? TitleAr, string Url, string? Note);
}
