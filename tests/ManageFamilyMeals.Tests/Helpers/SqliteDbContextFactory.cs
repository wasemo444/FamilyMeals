using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Shared.Models;
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
}
