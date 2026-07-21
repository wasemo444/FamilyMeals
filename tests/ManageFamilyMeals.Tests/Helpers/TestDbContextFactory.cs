using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Tests.Helpers;

internal static class TestDbContextFactory
{
    public static AppDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
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
