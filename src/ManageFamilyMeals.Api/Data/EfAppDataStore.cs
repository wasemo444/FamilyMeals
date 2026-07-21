using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Configurations;
using ManageFamilyMeals.Api.Mapping;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Data;

public sealed class EfAppDataStore(AppDbContext dbContext) : IAppDataStore
{
    public async Task<AppData?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var links = await dbContext.Links
            .AsNoTracking()
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
        var supportsTransactions = dbContext.Database.IsRelational()
            && dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        await using var transaction = supportsTransactions
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var existingCategoryIds = await dbContext.Categories
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);

        var existingLinkIds = await dbContext.Links
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
                dbContext.Categories.Add(category.ToEntity());
            }
            else
            {
                entity.Name = category.Name;
                entity.IsFavorite = category.IsFavorite;
                entity.CreatedAtUtc = category.CreatedAtUtc;
                entity.IsDeleted = category.IsDeleted;
                entity.DeletedAtUtc = category.DeletedAtUtc;
            }
        }

        foreach (var link in data.Links)
        {
            var entity = await dbContext.Links.FindAsync([link.Id], cancellationToken);
            if (entity is null)
            {
                dbContext.Links.Add(link.ToEntity());
            }
            else
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

        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
