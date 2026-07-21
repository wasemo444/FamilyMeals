using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Mapping;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Tests.Api;

public class EntityMappingExtensionsTests
{
    [Fact]
    public void MealCategoryEntity_ToModel_MapsAllFields()
    {
        // Arrange
        var entity = new MealCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Breakfast",
            IsFavorite = true,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var model = entity.ToModel();

        // Assert
        Assert.Equal(entity.Id, model.Id);
        Assert.Equal(entity.Name, model.Name);
        Assert.Equal(entity.IsFavorite, model.IsFavorite);
        Assert.Equal(entity.CreatedAtUtc, model.CreatedAtUtc);
        Assert.Equal(entity.IsDeleted, model.IsDeleted);
        Assert.Equal(entity.DeletedAtUtc, model.DeletedAtUtc);
    }

    [Fact]
    public void MealLink_RoundTripMapping_PreservesAllFields()
    {
        // Arrange
        var original = new MealLink
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            TitleEn = "Title EN",
            TitleAr = "Title AR",
            LegacyTitle = "Legacy",
            Url = "https://example.com",
            Note = "Note",
            IsFavorite = true,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false,
            PreviewTitle = "Preview",
            PreviewDescription = "Description",
            PreviewImageUrl = "https://example.com/image.jpg",
            PreviewSiteName = "Example"
        };

        // Act
        var roundTripped = original.ToEntity().ToModel();

        // Assert
        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.CategoryId, roundTripped.CategoryId);
        Assert.Equal(original.TitleEn, roundTripped.TitleEn);
        Assert.Equal(original.TitleAr, roundTripped.TitleAr);
        Assert.Equal(original.LegacyTitle, roundTripped.LegacyTitle);
        Assert.Equal(original.Url, roundTripped.Url);
        Assert.Equal(original.Note, roundTripped.Note);
        Assert.Equal(original.IsFavorite, roundTripped.IsFavorite);
        Assert.Equal(original.PreviewTitle, roundTripped.PreviewTitle);
        Assert.Equal(original.PreviewSiteName, roundTripped.PreviewSiteName);
    }

    [Fact]
    public void AppSettingsEntity_ToModel_MapsCultureCode()
    {
        // Arrange
        var entity = new AppSettingsEntity
        {
            Id = 1,
            CultureCode = "ar"
        };

        // Act
        var model = entity.ToModel();

        // Assert
        Assert.Equal("ar", model.CultureCode);
    }
}
