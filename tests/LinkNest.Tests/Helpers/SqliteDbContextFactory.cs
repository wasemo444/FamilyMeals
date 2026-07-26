using LinkNest.Api.Data;
using LinkNest.Api.Data.Configurations;
using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using LinkNest.Shared.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Tests.Helpers;

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
