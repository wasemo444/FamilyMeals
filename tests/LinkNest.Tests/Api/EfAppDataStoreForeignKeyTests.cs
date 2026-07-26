using LinkNest.Api.Data;
using LinkNest.Shared.Models;
using LinkNest.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Tests.Api;

public class EfAppDataStoreForeignKeyTests
{
    [Fact]
    public async Task SaveAsync_DeletesLinksBeforeCategoriesWhenBothAreRemoved()
    {
        // Arrange
        await using var context = SqliteDbContextFactory.CreateWithForeignKeys(nameof(SaveAsync_DeletesLinksBeforeCategoriesWhenBothAreRemoved));
        var store = TestServiceFactory.CreateEfAppDataStore(context);
        var categoryId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        await store.SaveAsync(new AppData
        {
            Categories = [new ContentCategory { Id = categoryId, Name = "Breakfast" }],
            Links =
            [
                new SavedLink
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
