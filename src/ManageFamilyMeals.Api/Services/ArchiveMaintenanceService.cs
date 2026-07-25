using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Services;

public sealed class ArchiveMaintenanceService(AppDbContext dbContext)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var threshold = ArchivePolicy.ExpirationThresholdUtc;

        var expiredCategoryIds = await dbContext.Categories
            .Where(category => category.IsDeleted
                && category.DeletedAtUtc != null
                && category.DeletedAtUtc < threshold)
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);

        if (expiredCategoryIds.Count > 0)
        {
            var linksInExpiredCategories = await dbContext.Links
                .Where(link => expiredCategoryIds.Contains(link.CategoryId))
                .ToListAsync(cancellationToken);

            if (linksInExpiredCategories.Count > 0)
            {
                dbContext.Links.RemoveRange(linksInExpiredCategories);
            }

            var expiredCategories = await dbContext.Categories
                .Where(category => expiredCategoryIds.Contains(category.Id))
                .ToListAsync(cancellationToken);

            dbContext.Categories.RemoveRange(expiredCategories);
        }

        var expiredLinks = await dbContext.Links
            .Where(link => link.IsDeleted
                && link.DeletedAtUtc != null
                && link.DeletedAtUtc < threshold)
            .ToListAsync(cancellationToken);

        if (expiredLinks.Count > 0)
        {
            dbContext.Links.RemoveRange(expiredLinks);
        }

        var legacyLinks = await dbContext.Links
            .Where(link => link.LegacyTitle != null && link.LegacyTitle != "" && link.TitleEn == "")
            .ToListAsync(cancellationToken);

        foreach (var link in legacyLinks)
        {
            link.TitleEn = link.LegacyTitle!;
            link.LegacyTitle = null;
        }

        if (expiredCategoryIds.Count > 0 || expiredLinks.Count > 0 || legacyLinks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
