using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Api.Mapping;

public static class EntityMappingExtensions
{
    public static MealCategory ToModel(this MealCategoryEntity entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsFavorite = entity.IsFavorite,
            CreatedAtUtc = entity.CreatedAtUtc,
            IsDeleted = entity.IsDeleted,
            DeletedAtUtc = entity.DeletedAtUtc
        };

    public static MealLink ToModel(this MealLinkEntity entity) =>
        new()
        {
            Id = entity.Id,
            CategoryId = entity.CategoryId,
            TitleEn = entity.TitleEn,
            TitleAr = entity.TitleAr,
            LegacyTitle = entity.LegacyTitle,
            Url = entity.Url,
            Note = entity.Note,
            IsFavorite = entity.IsFavorite,
            CreatedAtUtc = entity.CreatedAtUtc,
            IsDeleted = entity.IsDeleted,
            DeletedAtUtc = entity.DeletedAtUtc,
            PreviewTitle = entity.PreviewTitle,
            PreviewDescription = entity.PreviewDescription,
            PreviewImageUrl = entity.PreviewImageUrl,
            PreviewSiteName = entity.PreviewSiteName
        };

    public static MealCategoryEntity ToEntity(this MealCategory model) =>
        new()
        {
            Id = model.Id,
            Name = model.Name,
            IsFavorite = model.IsFavorite,
            CreatedAtUtc = model.CreatedAtUtc,
            IsDeleted = model.IsDeleted,
            DeletedAtUtc = model.DeletedAtUtc
        };

    public static MealLinkEntity ToEntity(this MealLink model) =>
        new()
        {
            Id = model.Id,
            CategoryId = model.CategoryId,
            TitleEn = model.TitleEn,
            TitleAr = model.TitleAr,
            LegacyTitle = model.LegacyTitle,
            Url = model.Url,
            Note = model.Note,
            IsFavorite = model.IsFavorite,
            CreatedAtUtc = model.CreatedAtUtc,
            IsDeleted = model.IsDeleted,
            DeletedAtUtc = model.DeletedAtUtc,
            PreviewTitle = model.PreviewTitle,
            PreviewDescription = model.PreviewDescription,
            PreviewImageUrl = model.PreviewImageUrl,
            PreviewSiteName = model.PreviewSiteName
        };

    public static AppSettings ToModel(this AppSettingsEntity entity) =>
        new()
        {
            CultureCode = entity.CultureCode
        };
}
