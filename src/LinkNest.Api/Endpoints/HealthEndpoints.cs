using LinkNest.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Health probe endpoints for load balancers and platform health checks.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps <c>/health</c> and optional <c>/health/ready</c> routes.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        endpoints.MapGet("/health/ready", ReadyAsync);
        endpoints.MapGet("/health/db", DbInfoAsync);
        return endpoints;
    }

    private static async Task<IResult> DbInfoAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var userCount = await dbContext.Users.CountAsync(cancellationToken);

        return Results.Ok(new
        {
            status = "ok",
            database = connection.Database,
            host = connection.DataSource,
            userCount
        });
    }

    private static async Task<IResult> ReadyAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "ready" })
            : Results.Problem(
                title: "Database unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
