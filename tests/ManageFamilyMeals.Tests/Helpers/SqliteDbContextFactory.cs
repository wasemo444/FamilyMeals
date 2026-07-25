using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Tests.Helpers;

internal static class SqliteDbContextFactory
{
    public static AppDbContext CreateWithForeignKeys(string databaseName)
    {
        var connection = new SqliteConnection($"Data Source={databaseName};Mode=Memory;Cache=Shared");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        SeedDefaultUser(context);

        if (!context.AppSettings.Any(item => item.Id == AppSettingsEntityConfiguration.SingletonId))
        {
            context.AppSettings.Add(new()
            {
                Id = AppSettingsEntityConfiguration.SingletonId,
                CultureCode = null
            });
            context.SaveChanges();
        }

        return context;
    }

    public static void SeedDefaultUser(AppDbContext context)
    {
        if (context.Users.Any(user => user.Id == WellKnownUsers.DefaultUserId))
        {
            return;
        }

        context.Users.Add(new ApplicationUser
        {
            Id = WellKnownUsers.DefaultUserId,
            UserName = WellKnownUsers.DefaultUserEmail,
            NormalizedUserName = WellKnownUsers.DefaultUserEmail.ToUpperInvariant(),
            Email = WellKnownUsers.DefaultUserEmail,
            NormalizedEmail = WellKnownUsers.DefaultUserEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        context.SaveChanges();
    }
}
