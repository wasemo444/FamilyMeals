using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Data;

/// <summary>
/// Entity Framework Core database context for meal data, groups, settings, and ASP.NET Identity.
/// </summary>
/// <remarks>
/// Entity configurations are applied from this assembly via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>.
/// </remarks>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    /// <summary>Meal category rows, including soft-delete and ownership columns.</summary>
    public DbSet<MealCategoryEntity> Categories => Set<MealCategoryEntity>();

    /// <summary>Meal link rows belonging to categories.</summary>
    public DbSet<MealLinkEntity> Links => Set<MealLinkEntity>();

    /// <summary>Collaboration groups with invite codes.</summary>
    public DbSet<GroupEntity> Groups => Set<GroupEntity>();

    /// <summary>User membership and role within a group.</summary>
    public DbSet<GroupMembershipEntity> GroupMemberships => Set<GroupMembershipEntity>();

    /// <summary>Singleton application settings row.</summary>
    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<IdentityPasskeyData>();
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
