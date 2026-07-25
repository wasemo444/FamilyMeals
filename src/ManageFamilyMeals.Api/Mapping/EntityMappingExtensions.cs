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
            DeletedAtUtc = entity.DeletedAtUtc,
            OwnerType = entity.OwnerType,
            OwnerUserId = entity.OwnerUserId,
            OwnerGroupId = entity.OwnerGroupId,
            RowVersion = entity.RowVersion
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
            PreviewSiteName = entity.PreviewSiteName,
            OwnerType = entity.OwnerType,
            OwnerUserId = entity.OwnerUserId,
            OwnerGroupId = entity.OwnerGroupId,
            RowVersion = entity.RowVersion
        };

    public static MealCategoryEntity ToEntity(this MealCategory model) =>
        new()
        {
            Id = model.Id,
            Name = model.Name,
            IsFavorite = model.IsFavorite,
            CreatedAtUtc = model.CreatedAtUtc,
            IsDeleted = model.IsDeleted,
            DeletedAtUtc = model.DeletedAtUtc,
            OwnerType = model.OwnerType,
            OwnerUserId = model.OwnerUserId,
            OwnerGroupId = model.OwnerGroupId,
            RowVersion = model.RowVersion
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
            PreviewSiteName = model.PreviewSiteName,
            OwnerType = model.OwnerType,
            OwnerUserId = model.OwnerUserId,
            OwnerGroupId = model.OwnerGroupId,
            RowVersion = model.RowVersion
        };

    public static GroupSummary ToSummary(this GroupEntity entity, GroupRole currentUserRole) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            InviteCode = entity.InviteCode,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedAtUtc = entity.CreatedAtUtc,
            CurrentUserRole = currentUserRole
        };

    public static AppSettings ToModel(this AppSettingsEntity entity) =>
        new()
        {
            CultureCode = entity.CultureCode
        };
}
