using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Api.Endpoints;

public static class BootstrapEndpoints
{
    public static IEndpointRouteBuilder MapBootstrapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/bootstrap", async (IMealDataService dataService, CancellationToken cancellationToken) =>
        {
            await dataService.EnsureLoadedAsync(cancellationToken);
            return Results.Ok(dataService.GetSnapshot());
        });

        return endpoints;
    }
}
