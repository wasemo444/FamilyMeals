using ManageFamilyMeals.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MealCategoryEntity> Categories => Set<MealCategoryEntity>();

    public DbSet<MealLinkEntity> Links => Set<MealLinkEntity>();

    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
