using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Tests.Api;

public class EfAppDataStoreForeignKeyTests
{
    [Fact]
    public async Task SaveAsync_DeletesLinksBeforeCategoriesWhenBothAreRemoved()
    {
        // Arrange
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(nameof(SaveAsync_DeletesLinksBeforeCategoriesWhenBothAreRemoved));
        var store = new EfAppDataStore(context);
        var categoryId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        await store.SaveAsync(new AppData
        {
            Categories = [new MealCategory { Id = categoryId, Name = "Breakfast" }],
            Links =
            [
                new MealLink
                {
                    Id = linkId,
                    CategoryId = categoryId,
                    TitleEn = "Pancakes",
                    Url = "https://example.com/pancakes"
                }
            ]
        });

        // Act
        await store.SaveAsync(new AppData());

        // Assert
        Assert.Empty(await context.Categories.ToListAsync());
        Assert.Empty(await context.Links.ToListAsync());
    }
}
