using LinkNest.Api.Data;
using LinkNest.Api.Data.Configurations;
using LinkNest.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Tests.Helpers;

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
