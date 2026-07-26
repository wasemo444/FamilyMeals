using LinkNest.Shared.Models;
using LinkNest.Shared.Services;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Provides the initial data snapshot endpoint used by the client on startup.
/// </summary>
/// <remarks>
/// Requires authentication. Meal data is scoped to the current user and their group memberships.
/// </remarks>
public static class BootstrapEndpoints
{
    /// <summary>
    /// Maps <c>GET /api/bootstrap</c>, which loads meal data and returns the full snapshot.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapBootstrapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/bootstrap", async (IContentDataService dataService, CancellationToken cancellationToken) =>
        {
            await dataService.EnsureLoadedAsync(cancellationToken);
            return Results.Ok(dataService.GetSnapshot());
        })
        .RequireAuthorization();

        return endpoints;
    }
}
