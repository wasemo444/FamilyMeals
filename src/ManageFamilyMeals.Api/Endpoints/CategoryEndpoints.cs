using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories").RequireAuthorization();

        group.MapGet("/", (HomeContentFilter? filter, IMealDataService dataService) =>
            Results.Ok(dataService.GetActiveCategories(filter ?? HomeContentFilter.All)));

        group.MapGet("/favorites", (HomeContentFilter? filter, IMealDataService dataService) =>
            Results.Ok(dataService.GetFavoriteCategories(filter ?? HomeContentFilter.All)));

        group.MapGet("/archived", (IMealDataService dataService) => Results.Ok(dataService.GetArchivedCategories()));

        group.MapGet("/name-available", async (
            string name,
            OwnerType? ownerType,
            Guid? ownerGroupId,
            IMealDataService dataService,
            IOwnershipAuthorization ownershipAuthorization,
            CancellationToken cancellationToken) =>
        {
            if (!ContentOwner.TryResolve(ownerType, ownerGroupId, out var owner, out var error))
            {
                return Results.BadRequest(new { error });
            }

            try
            {
                await ownershipAuthorization.ValidateCreateOwnerAsync(owner, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { available = !dataService.IsCategoryNameTaken(name, owner) });
        });

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

            if (!ContentOwner.TryResolve(request.OwnerType, request.OwnerGroupId, out var owner, out var ownerError))
            {
                return Results.BadRequest(new { error = ownerError });
            }

            if (dataService.IsCategoryNameTaken(request.Name, owner))
            {
                return Results.Conflict(new { error = "Category name already exists." });
            }

            try
            {
                var category = await dataService.AddCategoryAsync(request.Name, owner, cancellationToken);
                return Results.Created($"/api/categories/{category.Id}", category);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/{id:guid}/archive", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            try
            {
                var archived = await dataService.ArchiveCategoryAsync(id, cancellationToken);
                return archived ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/{id:guid}/restore", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            try
            {
                var restored = await dataService.RestoreCategoryAsync(id, cancellationToken);
                return restored ? Results.NoContent() : Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/{id:guid}/favorite", async (Guid id, IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            try
            {
                await dataService.ToggleCategoryFavoriteAsync(id, cancellationToken);
                var category = dataService.GetCategory(id);
                return category is null ? Results.NotFound() : Results.Ok(category);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.NotFound();
            }
        });

        return endpoints;
    }

    private sealed record CreateCategoryRequest(string Name, OwnerType? OwnerType = null, Guid? OwnerGroupId = null);
}
