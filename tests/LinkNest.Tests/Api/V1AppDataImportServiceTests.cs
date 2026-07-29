using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using LinkNest.Api.V1Import;
using LinkNest.Shared.Models;
using LinkNest.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinkNest.Tests.Api;

public class V1AppDataImportServiceTests
{
    [Fact]
    public async Task ImportAsync_PreservesFavoritesTimestampsAndArchiveState()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ImportAsync_PreservesFavoritesTimestampsAndArchiveState));
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "importer@test.local",
            Email = "importer@test.local",
            EmailConfirmed = true
        });
        await dbContext.SaveChangesAsync();

        var createdAt = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var deletedAt = new DateTime(2024, 6, 8, 12, 0, 0, DateTimeKind.Utc);
        var categoryId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        var source = new AppData
        {
            Categories =
            [
                new ContentCategory
                {
                    Id = categoryId,
                    Name = "Weeknight",
                    IsFavorite = true,
                    CreatedAtUtc = createdAt,
                    IsDeleted = true,
                    DeletedAtUtc = deletedAt
                }
            ],
            Links =
            [
                new SavedLink
                {
                    Id = linkId,
                    CategoryId = categoryId,
                    TitleEn = "Pasta",
                    Url = "https://example.com/pasta",
                    IsFavorite = true,
                    CreatedAtUtc = createdAt,
                    IsDeleted = true,
                    DeletedAtUtc = deletedAt
                }
            ]
        };

        var service = new V1AppDataImportService(dbContext, NullLogger<V1AppDataImportService>.Instance);
        var result = await service.ImportAsync(source, userId);

        Assert.Equal(1, result.CategoriesImported);
        Assert.Equal(1, result.LinksImported);

        var category = dbContext.Categories.Single();
        Assert.Equal(OwnerType.User, category.OwnerType);
        Assert.Equal(userId, category.OwnerUserId);
        Assert.True(category.IsFavorite);
        Assert.Equal(createdAt, category.CreatedAtUtc);
        Assert.True(category.IsDeleted);
        Assert.Equal(deletedAt, category.DeletedAtUtc);

        var link = dbContext.Links.Single();
        Assert.Equal(OwnerType.User, link.OwnerType);
        Assert.Equal(userId, link.OwnerUserId);
        Assert.True(link.IsFavorite);
        Assert.Equal(createdAt, link.CreatedAtUtc);
        Assert.True(link.IsDeleted);
        Assert.Equal(deletedAt, link.DeletedAtUtc);
    }

    [Fact]
    public async Task ImportAsync_IsIdempotentByPrimaryKey()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ImportAsync_IsIdempotentByPrimaryKey));
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "importer@test.local",
            Email = "importer@test.local",
            EmailConfirmed = true
        });
        await dbContext.SaveChangesAsync();

        var categoryId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var source = new AppData
        {
            Categories = [new ContentCategory { Id = categoryId, Name = "Lunch" }],
            Links = [new SavedLink { Id = linkId, CategoryId = categoryId, TitleEn = "Salad", Url = "https://example.com/s" }]
        };

        var service = new V1AppDataImportService(dbContext, NullLogger<V1AppDataImportService>.Instance);

        var first = await service.ImportAsync(source, userId);
        var second = await service.ImportAsync(source, userId);

        Assert.Equal(1, first.CategoriesImported);
        Assert.Equal(1, first.LinksImported);
        Assert.Equal(1, second.CategoriesSkipped);
        Assert.Equal(1, second.LinksSkipped);
        Assert.Equal(1, dbContext.Categories.Count());
        Assert.Equal(1, dbContext.Links.Count());
    }

    [Fact]
    public async Task ImportFromJsonAsync_MapsLegacyTitleField()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ImportFromJsonAsync_MapsLegacyTitleField));
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "importer@test.local",
            Email = "importer@test.local",
            EmailConfirmed = true
        });
        await dbContext.SaveChangesAsync();

        var categoryId = Guid.NewGuid();
        var json = $$"""
            {
              "categories": [{ "id": "{{categoryId}}", "name": "Legacy" }],
              "links": [{
                "id": "{{Guid.NewGuid()}}",
                "categoryId": "{{categoryId}}",
                "title": "Old Title",
                "url": "https://example.com/old"
              }]
            }
            """;

        var service = new V1AppDataImportService(dbContext, NullLogger<V1AppDataImportService>.Instance);
        var result = await service.ImportFromJsonAsync(json, userId);

        Assert.Equal(1, result.LinksImported);
        Assert.Equal("Old Title", dbContext.Links.Single().TitleEn);
    }

    [Fact]
    public async Task ImportAsync_SkipsLinksWhenCategoryOwnedByAnotherUser()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ImportAsync_SkipsLinksWhenCategoryOwnedByAnotherUser));
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        dbContext.Users.AddRange(
            new ApplicationUser
            {
                Id = targetUserId,
                UserName = "target@test.local",
                Email = "target@test.local",
                EmailConfirmed = true
            },
            new ApplicationUser
            {
                Id = otherUserId,
                UserName = "other@test.local",
                Email = "other@test.local",
                EmailConfirmed = true
            });
        await dbContext.SaveChangesAsync();

        var categoryId = Guid.NewGuid();
        dbContext.Categories.Add(new ContentCategoryEntity
        {
            Id = categoryId,
            Name = "Existing",
            OwnerType = OwnerType.User,
            OwnerUserId = otherUserId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var source = new AppData
        {
            Categories = [new ContentCategory { Id = categoryId, Name = "Existing" }],
            Links = [new SavedLink { Id = Guid.NewGuid(), CategoryId = categoryId, TitleEn = "Conflict", Url = "https://example.com/x" }]
        };

        var service = new V1AppDataImportService(dbContext, NullLogger<V1AppDataImportService>.Instance);
        var result = await service.ImportAsync(source, targetUserId);

        Assert.Equal(1, result.CategoriesSkipped);
        Assert.Equal(1, result.LinksSkipped);
        Assert.Equal(0, result.LinksImported);
        Assert.Empty(dbContext.Links);
    }
}
