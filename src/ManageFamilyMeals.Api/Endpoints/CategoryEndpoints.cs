using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories").RequireAuthorization();

        group.MapGet("/", (IMealDataService dataService) => Results.Ok(dataService.GetActiveCategories()));
        group.MapGet("/favorites", (IMealDataService dataService) => Results.Ok(dataService.GetFavoriteCategories()));
        group.MapGet("/archived", (IMealDataService dataService) => Results.Ok(dataService.GetArchivedCategories()));
        group.MapGet("/name-available", (string name, IMealDataService dataService) =>
            Results.Ok(new { available = !dataService.IsCategoryNameTaken(name) }));

        group.MapGet("/{id:guid}", (Guid id, IMealDataService dataService) =>
        {
            var category = dataService.GetCategory(id);
            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        group.MapPost("/", async (CreateCategoryRequest request, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }

            if (dataService.IsCategoryNameTaken(request.Name))
            {
                return Results.Conflict(new { error = "Category name already exists." });
            }

            var category = await dataService.AddCategoryAsync(request.Name, cancellationToken);
            return Results.Created($"/api/categories/{category.Id}", category);
        });

        group.MapPost("/{id:guid}/archive", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            var archived = await dataService.ArchiveCategoryAsync(id, cancellationToken);
            return archived ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/restore", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            var restored = await dataService.RestoreCategoryAsync(id, cancellationToken);
            return restored ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/favorite", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            await dataService.ToggleCategoryFavoriteAsync(id, cancellationToken);
            var category = dataService.GetCategory(id);
            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        return endpoints;
    }

    private sealed record CreateCategoryRequest(string Name);
}
