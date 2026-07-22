using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<MealCategoryEntity> Categories => Set<MealCategoryEntity>();

    public DbSet<MealLinkEntity> Links => Set<MealLinkEntity>();

    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<IdentityPasskeyData>();
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
