using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Api.Mapping;
using ManageFamilyMeals.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Endpoints;

public static class SettingsEndpoints
{
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
