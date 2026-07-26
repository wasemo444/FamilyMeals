using LinkNest.Api.Data;
using LinkNest.Api.Data.Configurations;
using LinkNest.Api.Mapping;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Minimal API routes for reading and updating application-wide settings (singleton row).
/// </summary>
/// <remarks>
/// Requires authentication. Settings are not ownership-scoped; any authenticated user may read and update.
/// </remarks>
public static class SettingsEndpoints
{
    /// <summary>
    /// Maps <c>GET</c> and <c>PUT /api/settings</c> for the culture and other app settings.
    /// </summary>
    /// <param name="endpoints">The application endpoint builder.</param>
    /// <returns>The same <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/settings").RequireAuthorization();

        group.MapGet("/", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var settings = await dbContext.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == AppSettingsEntityConfiguration.SingletonId, cancellationToken);

            return Results.Ok(settings?.ToModel() ?? new AppSettings());
        });

        group.MapPut("/", async (
            AppSettings request,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var settings = await dbContext.AppSettings
                .FirstOrDefaultAsync(item => item.Id == AppSettingsEntityConfiguration.SingletonId, cancellationToken);

            if (settings is null)
            {
                dbContext.AppSettings.Add(new()
                {
                    Id = AppSettingsEntityConfiguration.SingletonId,
                    CultureCode = request.CultureCode
                });
            }
            else
            {
                settings.CultureCode = request.CultureCode;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(request);
        });

        return endpoints;
    }
}
