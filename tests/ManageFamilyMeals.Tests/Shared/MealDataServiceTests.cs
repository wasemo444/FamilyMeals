using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Tests.Helpers;

namespace ManageFamilyMeals.Tests.Shared;

public class MealDataServiceTests
{
    [Fact]
    public async Task AddCategoryAsync_ThrowsWhenNameAlreadyExists()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        var service = new MealDataService(store);
        await service.InitializeAsync();
        await service.AddCategoryAsync("Breakfast");

        // Act
        var act = () => service.AddCategoryAsync("breakfast");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ArchiveCategoryAsync_CascadesSoftDeleteToActiveLinks()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        var service = new MealDataService(store);
        await service.InitializeAsync();
        var category = await service.AddCategoryAsync("Breakfast");
        await service.AddLinkAsync(category.Id, "Pancakes", "فطائر", "https://example.com/pancakes");

        // Act
        await service.ArchiveCategoryAsync(category.Id);

        // Assert
        Assert.Empty(service.GetActiveLinks(category.Id));
        Assert.Single(service.GetArchivedLinks(category.Id));
    }

    [Fact]
    public async Task RunMaintenanceAsync_PurgesItemsArchivedBeyondRetentionPeriod()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        await store.SaveAsync(new AppData
        {
            Categories =
            [
                new MealCategory
                {
                    Name = "Expired",
                    IsDeleted = true,
                    DeletedAtUtc = ArchivePolicy.ExpirationThresholdUtc.AddDays(-1)
                },
                new MealCategory
                {
                    Name = "Recent",
                    IsDeleted = true,
                    DeletedAtUtc = DateTime.UtcNow.AddDays(-1)
                }
            ]
        });
        var service = new MealDataService(store);

        // Act
        await service.RunMaintenanceAsync();

        // Assert
        var archived = service.GetArchivedCategories();
        Assert.Single(archived);
        Assert.Equal("Recent", archived[0].Name);
    }

    [Fact]
    public async Task RunMaintenanceAsync_MigratesLegacyTitleIntoTitleEn()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var store = new InMemoryAppDataStore();
        await store.SaveAsync(new AppData
        {
            Links =
            [
                new MealLink
                {
                    Id = linkId,
                    LegacyTitle = "Legacy name",
                    TitleEn = string.Empty
                }
            ]
        });
        var service = new MealDataService(store);

        // Act
        await service.RunMaintenanceAsync();

        // Assert
        var link = service.GetLink(linkId);
        Assert.NotNull(link);
        Assert.Equal("Legacy name", link!.TitleEn);
        Assert.Null(link.LegacyTitle);
    }

    [Fact]
    public async Task ToggleCategoryFavoriteAsync_FlipsFavoriteFlag()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        var service = new MealDataService(store);
        await service.InitializeAsync();
        var category = await service.AddCategoryAsync("Breakfast");

        // Act
        await service.ToggleCategoryFavoriteAsync(category.Id);

        // Assert
        var updated = service.GetCategory(category.Id);
        Assert.NotNull(updated);
        Assert.True(updated!.IsFavorite);
    }

    [Fact]
    public async Task EnsureLoadedAsync_DoesNotPurgeExpiredArchive()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        await store.SaveAsync(new AppData
        {
            Categories =
            [
                new MealCategory
                {
                    Name = "Expired",
                    IsDeleted = true,
                    DeletedAtUtc = ArchivePolicy.ExpirationThresholdUtc.AddDays(-1)
                }
            ]
        });
        var service = new MealDataService(store);

        // Act
        await service.EnsureLoadedAsync();

        // Assert
        Assert.Single(service.GetArchivedCategories());
    }

    [Fact]
    public async Task AddLinkAsync_ThrowsWhenCategoryMissing()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        var service = new MealDataService(store);
        await service.EnsureLoadedAsync();

        // Act
        var act = () => service.AddLinkAsync(Guid.NewGuid(), "Title", "عنوان", "https://example.com");

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }

    [Fact]
    public async Task ArchiveCategoryAsync_ReturnsFalseWhenCategoryMissing()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        var service = new MealDataService(store);
        await service.EnsureLoadedAsync();

        // Act
        var archived = await service.ArchiveCategoryAsync(Guid.NewGuid());

        // Assert
        Assert.False(archived);
    }

    [Fact]
    public async Task UpdateLinkPreviewAsync_FillsEmptyTitleEnFromPreviewTitle()
    {
        // Arrange
        var store = new InMemoryAppDataStore();
        var service = new MealDataService(store);
        await service.InitializeAsync();
        var category = await service.AddCategoryAsync("Breakfast");
        var link = await service.AddLinkAsync(category.Id, string.Empty, string.Empty, "https://example.com");

        // Act
        await service.UpdateLinkPreviewAsync(link.Id, new LinkPreviewData
        {
            Title = "Preview title",
            Description = "Description",
            ImageUrl = "https://example.com/image.jpg",
            SiteName = "Example"
        });

        // Assert
        var updated = service.GetLink(link.Id);
        Assert.NotNull(updated);
        Assert.Equal("Preview title", updated!.TitleEn);
        Assert.Equal("Description", updated.PreviewDescription);
    }
}
