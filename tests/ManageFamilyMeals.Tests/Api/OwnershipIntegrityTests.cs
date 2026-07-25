using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Tests.Api;

public class OwnershipIntegrityTests
{
    [Fact]
    public async Task SaveAsync_WithStaleRowVersion_ThrowsConcurrencyConflictException()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(SaveAsync_WithStaleRowVersion_ThrowsConcurrencyConflictException));
        var store = new EfAppDataStore(context, new TestCurrentUserContext());
        var categoryId = Guid.NewGuid();
        var category = new MealCategory { Id = categoryId, Name = "Breakfast" };
        TestOwnershipDefaults.ApplyUserOwnership(category);

        await store.SaveAsync(new AppData { Categories = [category] });
        var loaded = await store.LoadAsync();
        var staleVersion = loaded!.Categories[0].RowVersion;

        var firstUpdate = new AppData
        {
            Categories =
            [
                new MealCategory
                {
                    Id = categoryId,
                    Name = "Breakfast Updated",
                    RowVersion = staleVersion
                }
            ]
        };
        TestOwnershipDefaults.ApplyUserOwnership(firstUpdate.Categories[0]);
        await store.SaveAsync(firstUpdate);

        var staleUpdate = new AppData
        {
            Categories =
            [
                new MealCategory
                {
                    Id = categoryId,
                    Name = "Stale Update",
                    RowVersion = staleVersion
                }
            ]
        };
        TestOwnershipDefaults.ApplyUserOwnership(staleUpdate.Categories[0]);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => store.SaveAsync(staleUpdate));
    }

    [Fact]
    public async Task LoadAsync_ReturnsOnlyCurrentUserOwnedCategories()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(LoadAsync_ReturnsOnlyCurrentUserOwnedCategories));
        var otherUserId = Guid.NewGuid();
        context.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            UserName = "other@example.com",
            NormalizedUserName = "OTHER@EXAMPLE.COM",
            Email = "other@example.com",
            NormalizedEmail = "OTHER@EXAMPLE.COM",
            EmailConfirmed = true
        });

        context.Categories.AddRange(
            new MealCategoryEntity
            {
                Id = Guid.NewGuid(),
                Name = "Mine",
                OwnerType = OwnerType.User,
                OwnerUserId = WellKnownUsers.DefaultUserId,
                CreatedAtUtc = DateTime.UtcNow
            },
            new MealCategoryEntity
            {
                Id = Guid.NewGuid(),
                Name = "Theirs",
                OwnerType = OwnerType.User,
                OwnerUserId = otherUserId,
                CreatedAtUtc = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var store = new EfAppDataStore(context, new TestCurrentUserContext());
        var data = await store.LoadAsync();

        Assert.NotNull(data);
        Assert.Single(data!.Categories);
        Assert.Equal("Mine", data.Categories[0].Name);
    }

    [Fact]
    public async Task SaveChanges_RejectsInvalidOwnerTypeCombination()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(SaveChanges_RejectsInvalidOwnerTypeCombination));

        context.Categories.Add(new MealCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Invalid",
            OwnerType = OwnerType.User,
            OwnerUserId = null,
            OwnerGroupId = null,
            CreatedAtUtc = DateTime.UtcNow
        });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveAsync_WithEmptyRowVersionOnUpdate_ThrowsConcurrencyConflictException()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(SaveAsync_WithEmptyRowVersionOnUpdate_ThrowsConcurrencyConflictException));
        var store = new EfAppDataStore(context, new TestCurrentUserContext());
        var categoryId = Guid.NewGuid();
        var category = new MealCategory { Id = categoryId, Name = "Breakfast" };
        TestOwnershipDefaults.ApplyUserOwnership(category);

        await store.SaveAsync(new AppData { Categories = [category] });

        var update = new AppData
        {
            Categories =
            [
                new MealCategory
                {
                    Id = categoryId,
                    Name = "Breakfast Updated",
                    RowVersion = []
                }
            ]
        };
        TestOwnershipDefaults.ApplyUserOwnership(update.Categories[0]);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => store.SaveAsync(update));
    }

    [Fact]
    public async Task SaveAsync_WithForeignOwnerUserId_StoresCurrentUserOwnership()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(SaveAsync_WithForeignOwnerUserId_StoresCurrentUserOwnership));
        var otherUserId = Guid.NewGuid();
        context.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            UserName = "other@example.com",
            NormalizedUserName = "OTHER@EXAMPLE.COM",
            Email = "other@example.com",
            NormalizedEmail = "OTHER@EXAMPLE.COM",
            EmailConfirmed = true
        });
        await context.SaveChangesAsync();

        var store = new EfAppDataStore(context, new TestCurrentUserContext());
        var category = new MealCategory
        {
            Id = Guid.NewGuid(),
            Name = "Breakfast",
            OwnerType = OwnerType.User,
            OwnerUserId = otherUserId
        };

        await store.SaveAsync(new AppData { Categories = [category] });

        var entity = await context.Categories.SingleAsync(item => item.Id == category.Id);
        Assert.Equal(WellKnownUsers.DefaultUserId, entity.OwnerUserId);
    }

    [Fact]
    public async Task DeleteUser_WithOwnedCategories_IsBlockedByForeignKey()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(DeleteUser_WithOwnedCategories_IsBlockedByForeignKey));

        context.Categories.Add(new MealCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Breakfast",
            OwnerType = OwnerType.User,
            OwnerUserId = WellKnownUsers.DefaultUserId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var user = await context.Users.SingleAsync(item => item.Id == WellKnownUsers.DefaultUserId);
        context.Users.Remove(user);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteGroup_WithOwnedCategories_IsBlockedByForeignKey()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(DeleteGroup_WithOwnedCategories_IsBlockedByForeignKey));
        var groupId = Guid.NewGuid();

        context.Groups.Add(new GroupEntity
        {
            Id = groupId,
            Name = "Family",
            InviteCode = "TESTCODE",
            CreatedByUserId = WellKnownUsers.DefaultUserId,
            CreatedAtUtc = DateTime.UtcNow
        });
        context.Categories.Add(new MealCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Shared Breakfast",
            OwnerType = OwnerType.Group,
            OwnerGroupId = groupId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var group = await context.Groups.SingleAsync(item => item.Id == groupId);
        context.Groups.Remove(group);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
