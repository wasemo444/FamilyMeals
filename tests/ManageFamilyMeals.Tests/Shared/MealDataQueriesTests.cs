using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Tests.Shared;

public class MealDataQueriesTests
{
    [Fact]
    public void GetActiveCategories_ExcludesDeletedCategories()
    {
        // Arrange
        var data = new AppData
        {
            Categories =
            [
                new MealCategory { Name = "Active", IsDeleted = false },
                new MealCategory { Name = "Archived", IsDeleted = true }
            ]
        };

        // Act
        var result = MealDataQueries.GetActiveCategories(data);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public void SortCategories_PlacesFavoritesFirstThenSortsByName()
    {
        // Arrange
        var data = new AppData
        {
            Categories =
            [
                new MealCategory { Name = "Zulu", IsFavorite = false, IsDeleted = false },
                new MealCategory { Name = "Alpha", IsFavorite = true, IsDeleted = false },
                new MealCategory { Name = "Bravo", IsFavorite = false, IsDeleted = false }
            ]
        };

        // Act
        var result = MealDataQueries.GetActiveCategories(data);

        // Assert
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Bravo", result[1].Name);
        Assert.Equal("Zulu", result[2].Name);
    }

    [Fact]
    public void IsCategoryNameTaken_IsCaseInsensitiveAndIgnoresArchived()
    {
        // Arrange
        var data = new AppData
        {
            Categories =
            [
                new MealCategory { Name = "Breakfast", IsDeleted = false },
                new MealCategory { Name = "Lunch", IsDeleted = true }
            ]
        };

        // Act
        var activeDuplicate = MealDataQueries.IsCategoryNameTaken(data, "breakfast");
        var archivedName = MealDataQueries.IsCategoryNameTaken(data, "lunch");
        var available = MealDataQueries.IsCategoryNameTaken(data, "Dinner");

        // Assert
        Assert.True(activeDuplicate);
        Assert.False(archivedName);
        Assert.False(available);
    }

    [Fact]
    public void GetFavoriteLinks_ReturnsOnlyFavoriteActiveLinksForCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var data = new AppData
        {
            Links =
            [
                new MealLink { CategoryId = categoryId, TitleEn = "Fav", IsFavorite = true, IsDeleted = false },
                new MealLink { CategoryId = categoryId, TitleEn = "Plain", IsFavorite = false, IsDeleted = false },
                new MealLink { CategoryId = categoryId, TitleEn = "Archived", IsFavorite = true, IsDeleted = true },
                new MealLink { CategoryId = otherCategoryId, TitleEn = "Other", IsFavorite = true, IsDeleted = false }
            ]
        };

        // Act
        var result = MealDataQueries.GetFavoriteLinks(data, categoryId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Fav", result[0].TitleEn);
    }

    [Fact]
    public void GetArchivedCategories_OrdersByMostRecentlyDeletedFirst()
    {
        // Arrange
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);
        var data = new AppData
        {
            Categories =
            [
                new MealCategory { Name = "Old", IsDeleted = true, DeletedAtUtc = older },
                new MealCategory { Name = "New", IsDeleted = true, DeletedAtUtc = newer }
            ]
        };

        // Act
        var result = MealDataQueries.GetArchivedCategories(data);

        // Assert
        Assert.Equal("New", result[0].Name);
        Assert.Equal("Old", result[1].Name);
    }
}
