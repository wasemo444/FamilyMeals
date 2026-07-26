using LinkNest.Shared.Models;
using LinkNest.Shared.Services;

namespace LinkNest.Tests.Shared;

public class ContentDataQueriesTests
{
    [Fact]
    public void GetActiveCategories_ExcludesDeletedCategories()
    {
        // Arrange
        var data = new AppData
        {
            Categories =
            [
                new ContentCategory { Name = "Active", IsDeleted = false },
                new ContentCategory { Name = "Archived", IsDeleted = true }
            ]
        };

        // Act
        var result = ContentDataQueries.GetActiveCategories(data);

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
                new ContentCategory { Name = "Zulu", IsFavorite = false, IsDeleted = false },
                new ContentCategory { Name = "Alpha", IsFavorite = true, IsDeleted = false },
                new ContentCategory { Name = "Bravo", IsFavorite = false, IsDeleted = false }
            ]
        };

        // Act
        var result = ContentDataQueries.GetActiveCategories(data);

        // Assert
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Bravo", result[1].Name);
        Assert.Equal("Zulu", result[2].Name);
    }

    [Fact]
    public void IsCategoryNameTaken_IsScopedPerOwner()
    {
        var groupId = Guid.NewGuid();
        var data = new AppData
        {
            Categories =
            [
                new ContentCategory { Name = "Breakfast", OwnerType = OwnerType.User, IsDeleted = false },
                new ContentCategory
                {
                    Name = "Breakfast",
                    OwnerType = OwnerType.Group,
                    OwnerGroupId = groupId,
                    IsDeleted = false
                }
            ]
        };

        Assert.True(ContentDataQueries.IsCategoryNameTaken(data, "Breakfast", ContentOwner.Personal));
        Assert.True(ContentDataQueries.IsCategoryNameTaken(data, "Breakfast", ContentOwner.ForGroup(groupId)));
        Assert.False(ContentDataQueries.IsCategoryNameTaken(data, "Breakfast", ContentOwner.ForGroup(Guid.NewGuid())));
    }

    [Fact]
    public void GetActiveCategories_FiltersByHomeContentFilter()
    {
        var data = new AppData
        {
            Categories =
            [
                new ContentCategory { Name = "Personal", OwnerType = OwnerType.User, IsDeleted = false },
                new ContentCategory
                {
                    Name = "Shared",
                    OwnerType = OwnerType.Group,
                    OwnerGroupId = Guid.NewGuid(),
                    IsDeleted = false
                }
            ]
        };

        Assert.Equal(2, ContentDataQueries.GetActiveCategories(data, HomeContentFilter.All).Count);
        Assert.Single(ContentDataQueries.GetActiveCategories(data, HomeContentFilter.Mine));
        Assert.Single(ContentDataQueries.GetActiveCategories(data, HomeContentFilter.Shared));
    }

    [Fact]
    public void IsCategoryNameTaken_IsCaseInsensitiveAndIgnoresArchived()
    {
        // Arrange
        var data = new AppData
        {
            Categories =
            [
                new ContentCategory { Name = "Breakfast", IsDeleted = false },
                new ContentCategory { Name = "Lunch", IsDeleted = true }
            ]
        };

        // Act
        var activeDuplicate = ContentDataQueries.IsCategoryNameTaken(data, "breakfast", ContentOwner.Personal);
        var archivedName = ContentDataQueries.IsCategoryNameTaken(data, "lunch", ContentOwner.Personal);
        var available = ContentDataQueries.IsCategoryNameTaken(data, "Dinner", ContentOwner.Personal);

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
                new SavedLink { CategoryId = categoryId, TitleEn = "Fav", IsFavorite = true, IsDeleted = false },
                new SavedLink { CategoryId = categoryId, TitleEn = "Plain", IsFavorite = false, IsDeleted = false },
                new SavedLink { CategoryId = categoryId, TitleEn = "Archived", IsFavorite = true, IsDeleted = true },
                new SavedLink { CategoryId = otherCategoryId, TitleEn = "Other", IsFavorite = true, IsDeleted = false }
            ]
        };

        // Act
        var result = ContentDataQueries.GetFavoriteLinks(data, categoryId);

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
                new ContentCategory { Name = "Old", IsDeleted = true, DeletedAtUtc = older },
                new ContentCategory { Name = "New", IsDeleted = true, DeletedAtUtc = newer }
            ]
        };

        // Act
        var result = ContentDataQueries.GetArchivedCategories(data);

        // Assert
        Assert.Equal("New", result[0].Name);
        Assert.Equal("Old", result[1].Name);
    }
}
