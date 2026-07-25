using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Api.Services;
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
        var store = TestServiceFactory.CreateEfAppDataStore(context);
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

        var store = TestServiceFactory.CreateEfAppDataStore(context);
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
        var store = TestServiceFactory.CreateEfAppDataStore(context);
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

        var store = TestServiceFactory.CreateEfAppDataStore(context);
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
    public async Task SaveAsync_WhenLinkMovesCategory_RealignsOwnership()
    {
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(
            nameof(SaveAsync_WhenLinkMovesCategory_RealignsOwnership));
        var userContext = new TestCurrentUserContext();
        var store = new EfAppDataStore(
            context,
            userContext,
            new OwnershipAuthorizationService(context, userContext));
        var groupId = Guid.NewGuid();
        var personalCategoryId = Guid.NewGuid();
        var groupCategoryId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        context.Groups.Add(new GroupEntity
        {
            Id = groupId,
            Name = "Family",
            InviteCode = "TESTCODE",
            CreatedByUserId = WellKnownUsers.DefaultUserId,
            CreatedAtUtc = DateTime.UtcNow
        });
        context.GroupMemberships.Add(new GroupMembershipEntity
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = WellKnownUsers.DefaultUserId,
            Role = GroupRole.Admin,
            JoinedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var personalCategory = new MealCategory { Id = personalCategoryId, Name = "Personal" };
        TestOwnershipDefaults.ApplyUserOwnership(personalCategory);
        var groupCategory = new MealCategory
        {
            Id = groupCategoryId,
            Name = "Shared",
            OwnerType = OwnerType.Group,
            OwnerGroupId = groupId
        };
        var link = new MealLink
        {
            Id = linkId,
            CategoryId = personalCategoryId,
            TitleEn = "Recipe",
            Url = "https://example.com/recipe"
        };
        TestOwnershipDefaults.ApplyUserOwnership(link);

        await store.SaveAsync(new AppData
        {
            Categories = [personalCategory, groupCategory],
            Links = [link]
        });

        var loaded = await store.LoadAsync();
        var movedLink = loaded!.Links.Single(item => item.Id == linkId);
        movedLink.CategoryId = groupCategoryId;

        await store.SaveAsync(new AppData
        {
            Categories = loaded.Categories,
            Links = [movedLink]
        });

        var entity = await context.Links.SingleAsync(item => item.Id == linkId);
        Assert.Equal(OwnerType.Group, entity.OwnerType);
        Assert.Equal(groupId, entity.OwnerGroupId);
        Assert.Null(entity.OwnerUserId);
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
