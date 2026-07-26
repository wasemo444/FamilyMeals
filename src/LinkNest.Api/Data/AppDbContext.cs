using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.Data;

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
    public DbSet<ContentCategoryEntity> Categories => Set<ContentCategoryEntity>();

    /// <summary>Meal link rows belonging to categories.</summary>
    public DbSet<SavedLinkEntity> Links => Set<SavedLinkEntity>();

    /// <summary>Collaboration groups with invite codes.</summary>
    public DbSet<GroupEntity> Groups => Set<GroupEntity>();

    /// <summary>User membership and role within a group.</summary>
    public DbSet<GroupMembershipEntity> GroupMemberships => Set<GroupMembershipEntity>();

    /// <summary>Pending and historical email invites to join groups.</summary>
    public DbSet<GroupInviteEntity> GroupInvites => Set<GroupInviteEntity>();

    /// <summary>Singleton application settings row.</summary>
    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<IdentityPasskeyData>();
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
