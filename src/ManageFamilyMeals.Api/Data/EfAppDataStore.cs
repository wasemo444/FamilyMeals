using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Mapping;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ManageFamilyMeals.Api.Data;

public sealed class EfAppDataStore(AppDbContext dbContext, ICurrentUserContext currentUser) : IAppDataStore
{
    public async Task<AppData?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.OwnerType == OwnerType.User && category.OwnerUserId == userId)
            .ToListAsync(cancellationToken);

        var categoryIds = categories.Select(category => category.Id).ToHashSet();

        var links = await dbContext.Links
            .AsNoTracking()
            .Where(link => link.OwnerType == OwnerType.User
                && link.OwnerUserId == userId
                && categoryIds.Contains(link.CategoryId))
            .ToListAsync(cancellationToken);

        var settings = await dbContext.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == AppSettingsEntityConfiguration.SingletonId, cancellationToken);

        return new AppData
        {
            Categories = categories.Select(category => category.ToModel()).ToList(),
            Links = links.Select(link => link.ToModel()).ToList(),
            Settings = settings?.ToModel() ?? new AppSettings()
        };
    }

    public async Task SaveAsync(AppData data, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();

        var supportsTransactions = dbContext.Database.IsRelational()
            && dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        await using var transaction = supportsTransactions
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var existingCategoryIds = await dbContext.Categories
            .Where(category => category.OwnerType == OwnerType.User && category.OwnerUserId == userId)
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);

        var existingLinkIds = await dbContext.Links
            .Where(link => link.OwnerType == OwnerType.User && link.OwnerUserId == userId)
            .Select(link => link.Id)
            .ToListAsync(cancellationToken);

        var incomingCategoryIds = data.Categories.Select(category => category.Id).ToHashSet();
        var incomingLinkIds = data.Links.Select(link => link.Id).ToHashSet();

        foreach (var linkId in existingLinkIds.Where(id => !incomingLinkIds.Contains(id)))
        {
            var entity = await dbContext.Links.FindAsync([linkId], cancellationToken);
            if (entity is not null)
            {
                dbContext.Links.Remove(entity);
            }
        }

        foreach (var categoryId in existingCategoryIds.Where(id => !incomingCategoryIds.Contains(id)))
        {
            var entity = await dbContext.Categories.FindAsync([categoryId], cancellationToken);
            if (entity is not null)
            {
                dbContext.Categories.Remove(entity);
            }
        }

        foreach (var category in data.Categories)
        {
            var entity = await dbContext.Categories.FindAsync([category.Id], cancellationToken);
            if (entity is null)
            {
                var newEntity = category.ToEntity();
                StampUserOwnership(newEntity, userId);
                dbContext.Categories.Add(newEntity);
            }
            else if (entity.OwnerType == OwnerType.User && entity.OwnerUserId == userId)
            {
                entity.Name = category.Name;
                entity.IsFavorite = category.IsFavorite;
                entity.CreatedAtUtc = category.CreatedAtUtc;
                entity.IsDeleted = category.IsDeleted;
                entity.DeletedAtUtc = category.DeletedAtUtc;
                ApplyConcurrencyToken(dbContext.Entry(entity), category.RowVersion);
            }
        }

        foreach (var link in data.Links)
        {
            if (!incomingCategoryIds.Contains(link.CategoryId))
            {
                continue;
            }

            var entity = await dbContext.Links.FindAsync([link.Id], cancellationToken);
            if (entity is null)
            {
                var newEntity = link.ToEntity();
                StampUserOwnership(newEntity, userId);
                dbContext.Links.Add(newEntity);
            }
            else if (entity.OwnerType == OwnerType.User && entity.OwnerUserId == userId)
            {
                entity.CategoryId = link.CategoryId;
                entity.TitleEn = link.TitleEn;
                entity.TitleAr = link.TitleAr;
                entity.LegacyTitle = link.LegacyTitle;
                entity.Url = link.Url;
                entity.Note = link.Note;
                entity.IsFavorite = link.IsFavorite;
                entity.CreatedAtUtc = link.CreatedAtUtc;
                entity.IsDeleted = link.IsDeleted;
                entity.DeletedAtUtc = link.DeletedAtUtc;
                entity.PreviewTitle = link.PreviewTitle;
                entity.PreviewDescription = link.PreviewDescription;
                entity.PreviewImageUrl = link.PreviewImageUrl;
                entity.PreviewSiteName = link.PreviewSiteName;
                ApplyConcurrencyToken(dbContext.Entry(entity), link.RowVersion);
            }
        }

        var settings = await dbContext.AppSettings
            .FirstOrDefaultAsync(item => item.Id == AppSettingsEntityConfiguration.SingletonId, cancellationToken);

        if (settings is null)
        {
            dbContext.AppSettings.Add(new()
            {
                Id = AppSettingsEntityConfiguration.SingletonId,
                CultureCode = data.Settings.CultureCode
            });
        }
        else
        {
            settings.CultureCode = data.Settings.CultureCode;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static void StampUserOwnership(MealCategoryEntity entity, Guid userId)
    {
        entity.OwnerType = OwnerType.User;
        entity.OwnerUserId = userId;
        entity.OwnerGroupId = null;
    }

    private static void StampUserOwnership(MealLinkEntity entity, Guid userId)
    {
        entity.OwnerType = OwnerType.User;
        entity.OwnerUserId = userId;
        entity.OwnerGroupId = null;
    }

    private static void ApplyConcurrencyToken(EntityEntry entry, byte[]? clientRowVersion)
    {
        if (clientRowVersion is null || clientRowVersion.Length == 0)
        {
            throw new ConcurrencyConflictException(
                "A row version is required to update existing records. Reload and retry.");
        }

        var rowVersionProperty = entry.Property("RowVersion");
        rowVersionProperty.OriginalValue = clientRowVersion;
        rowVersionProperty.CurrentValue = IncrementRowVersion(clientRowVersion);
    }

    private static byte[] IncrementRowVersion(byte[] rowVersion)
    {
        var next = (byte[])rowVersion.Clone();
        for (var index = next.Length - 1; index >= 0; index--)
        {
            if (++next[index] != 0)
            {
                break;
            }
        }

        return next;
    }
}
