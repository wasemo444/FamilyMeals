using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Services;

public sealed class OwnershipBackfillService(AppDbContext dbContext)
{
    public async Task BackfillUnownedMealDataAsync(CancellationToken cancellationToken = default)
    {
        var hasUnownedCategories = await dbContext.Categories
            .AnyAsync(category => category.OwnerUserId == null, cancellationToken);
        var hasUnownedLinks = await dbContext.Links
            .AnyAsync(link => link.OwnerUserId == null, cancellationToken);

        if (!hasUnownedCategories && !hasUnownedLinks)
        {
            return;
        }

        var ownerUserId = await dbContext.Users
            .OrderBy(user => user.CreatedAtUtc)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerUserId == Guid.Empty)
        {
            return;
        }

        await dbContext.Categories
            .Where(category => category.OwnerUserId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(category => category.OwnerType, OwnerType.User)
                    .SetProperty(category => category.OwnerUserId, ownerUserId),
                cancellationToken);

        await dbContext.Links
            .Where(link => link.OwnerUserId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(link => link.OwnerType, OwnerType.User)
                    .SetProperty(link => link.OwnerUserId, ownerUserId),
                cancellationToken);
    }
}
