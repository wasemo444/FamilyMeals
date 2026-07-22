using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Startup;

public static class DatabaseStartupExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (dbContext.Database.IsRelational()
                    && dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
                {
                    await dbContext.Database.MigrateAsync();
                }
                else
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogError(
                    ex,
                    "Database migration failed. Ensure PostgreSQL is running (docker compose up) and the connection string is valid.");
                throw;
            }
        }
        else if (app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
            await seeder.SeedAsync();

            var mealDataService = scope.ServiceProvider.GetService<IMealDataService>();
            if (mealDataService is not null)
            {
                await mealDataService.RunMaintenanceAsync();
            }
        }
    }
}
