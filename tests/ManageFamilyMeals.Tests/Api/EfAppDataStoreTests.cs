using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Tests.Helpers;

namespace ManageFamilyMeals.Tests.Api;

public class EfAppDataStoreTests
{
    [Fact]
    public async Task LoadAsync_ReturnsEmptyAppDataWhenDatabaseIsEmpty()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create(nameof(LoadAsync_ReturnsEmptyAppDataWhenDatabaseIsEmpty));
        var store = new EfAppDataStore(context);

        // Act
        var data = await store.LoadAsync();

        // Assert
        Assert.NotNull(data);
        Assert.Empty(data!.Categories);
        Assert.Empty(data.Links);
        Assert.Null(data.Settings.CultureCode);
    }

    [Fact]
    public async Task SaveAsync_PersistsCategoriesLinksAndSettings()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create(nameof(SaveAsync_PersistsCategoriesLinksAndSettings));
        var store = new EfAppDataStore(context);
        var categoryId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var payload = new AppData
        {
            Categories =
            [
                new MealCategory { Id = categoryId, Name = "Breakfast" }
            ],
            Links =
            [
                new MealLink
                {
                    Id = linkId,
                    CategoryId = categoryId,
                    TitleEn = "Pancakes",
                    Url = "https://example.com/pancakes"
                }
            ],
            Settings = new AppSettings { CultureCode = "ar" }
        };

        // Act
        await store.SaveAsync(payload);
        var loaded = await store.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Single(loaded!.Categories);
        Assert.Equal("Breakfast", loaded.Categories[0].Name);
        Assert.Single(loaded.Links);
        Assert.Equal("Pancakes", loaded.Links[0].TitleEn);
        Assert.Equal("ar", loaded.Settings.CultureCode);
    }

    [Fact]
    public async Task SaveAsync_RemovesEntitiesMissingFromPayload()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create(nameof(SaveAsync_RemovesEntitiesMissingFromPayload));
        var store = new EfAppDataStore(context);
        var categoryId = Guid.NewGuid();
        await store.SaveAsync(new AppData
        {
            Categories = [new MealCategory { Id = categoryId, Name = "Breakfast" }]
        });

        // Act
        await store.SaveAsync(new AppData());
        var loaded = await store.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Empty(loaded!.Categories);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingRowsInsteadOfDuplicating()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create(nameof(SaveAsync_UpdatesExistingRowsInsteadOfDuplicating));
        var store = new EfAppDataStore(context);
        var categoryId = Guid.NewGuid();
        await store.SaveAsync(new AppData
        {
            Categories = [new MealCategory { Id = categoryId, Name = "Breakfast", IsFavorite = false }]
        });

        // Act
        await store.SaveAsync(new AppData
        {
            Categories = [new MealCategory { Id = categoryId, Name = "Breakfast", IsFavorite = true }]
        });
        var loaded = await store.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Single(loaded!.Categories);
        Assert.True(loaded.Categories[0].IsFavorite);
        Assert.Equal(AppSettingsEntityConfiguration.SingletonId, context.AppSettings.Single().Id);
    }
}
