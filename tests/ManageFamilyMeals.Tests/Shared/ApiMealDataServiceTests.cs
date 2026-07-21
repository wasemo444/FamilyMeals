using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Tests.Helpers;

namespace ManageFamilyMeals.Tests.Shared;

public class ApiMealDataServiceTests
{
    [Fact]
    public async Task InitializeAsync_LoadsBootstrapSnapshotIntoCache()
    {
        // Arrange
        var bootstrap = new AppData
        {
            Categories = [new MealCategory { Name = "Breakfast" }],
            Settings = new AppSettings { CultureCode = "en" }
        };
        var handler = new FakeHttpMessageHandler()
            .MapGet("/api/bootstrap", bootstrap);
        var service = new ApiMealDataService(new FakeHttpClientFactory(handler));

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.Single(service.GetActiveCategories());
        Assert.Equal("en", service.GetSettings().CultureCode);
    }

    [Fact]
    public async Task InitializeAsync_RaisesDataChangedOnce()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler()
            .MapGet("/api/bootstrap", new AppData());
        var service = new ApiMealDataService(new FakeHttpClientFactory(handler));
        var changeCount = 0;
        service.DataChanged += () => changeCount++;

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public async Task AddCategoryAsync_AppendsCategoryToCacheAndRaisesDataChanged()
    {
        // Arrange
        var created = new MealCategory { Name = "Lunch" };
        var handler = new FakeHttpMessageHandler()
            .MapGetSequence(
                "/api/bootstrap",
                new AppData(),
                new AppData { Categories = [created] })
            .MapPost("/api/categories", _ => created);
        var service = new ApiMealDataService(new FakeHttpClientFactory(handler));
        var changeCount = 0;
        service.DataChanged += () => changeCount++;
        await service.InitializeAsync();

        // Act
        var result = await service.AddCategoryAsync("Lunch");

        // Assert
        Assert.Equal("Lunch", result.Name);
        Assert.Contains(service.GetActiveCategories(), category => category.Name == "Lunch");
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public async Task ToggleCategoryFavoriteAsync_UpdatesCachedCategoryFromResponse()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler()
            .MapGetSequence(
                "/api/bootstrap",
                new AppData
                {
                    Categories = [new MealCategory { Id = categoryId, Name = "Breakfast", IsFavorite = false }]
                },
                new AppData
                {
                    Categories = [new MealCategory { Id = categoryId, Name = "Breakfast", IsFavorite = true }]
                })
            .MapPost("/api/categories/" + categoryId + "/favorite", _ =>
                new MealCategory { Id = categoryId, Name = "Breakfast", IsFavorite = true });
        var service = new ApiMealDataService(new FakeHttpClientFactory(handler));
        await service.InitializeAsync();

        // Act
        await service.ToggleCategoryFavoriteAsync(categoryId);

        // Assert
        var updated = service.GetCategory(categoryId);
        Assert.NotNull(updated);
        Assert.True(updated!.IsFavorite);
    }

    [Fact]
    public async Task ArchiveCategoryAsync_ReloadsCacheFromBootstrap()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler()
            .MapGetSequence(
                "/api/bootstrap",
                new AppData
                {
                    Categories = [new MealCategory { Id = categoryId, Name = "Breakfast", IsDeleted = false }]
                },
                new AppData
                {
                    Categories = [new MealCategory { Id = categoryId, Name = "Breakfast", IsDeleted = true }]
                })
            .MapPostNoContent("/api/categories/" + categoryId + "/archive");
        var service = new ApiMealDataService(new FakeHttpClientFactory(handler));
        await service.InitializeAsync();

        // Act
        await service.ArchiveCategoryAsync(categoryId);

        // Assert
        Assert.Empty(service.GetActiveCategories());
        Assert.Single(service.GetArchivedCategories());
    }

    [Fact]
    public async Task ApplySettings_UpdatesCachedCultureWithoutReload()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler()
            .MapGet("/api/bootstrap", new AppData { Settings = new AppSettings { CultureCode = "en" } });
        var service = new ApiMealDataService(new FakeHttpClientFactory(handler));
        await service.InitializeAsync();

        // Act
        service.ApplySettings(new AppSettings { CultureCode = "ar" });

        // Assert
        Assert.Equal("ar", service.GetSettings().CultureCode);
    }
}
