using LinkNest.Api.Data;
using LinkNest.Shared.Constants;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.Services;

/// <summary>
/// One-time migration helper that assigns ownership to legacy categories and links created before ownership was enforced.
/// </summary>
/// <remarks>
/// Invoked at application startup after identity seeding. Unowned categories receive the default or earliest user;
/// unowned links inherit ownership from their parent category.
/// </remarks>
public sealed class OwnershipBackfillService(AppDbContext dbContext)
{
    /// <summary>
    /// Backfills <see cref="OwnerType"/> and owner foreign keys on rows with null ownership columns.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel database operations.</param>
    /// <returns>A task that completes when backfill finishes (no-op if no unowned rows exist).</returns>
    public async Task BackfillUnownedMealDataAsync(CancellationToken cancellationToken = default)
    {
        var hasUnownedCategories = await dbContext.Categories
            .AnyAsync(category => category.OwnerUserId == null && category.OwnerGroupId == null, cancellationToken);
        var hasUnownedLinks = await dbContext.Links
            .AnyAsync(link => link.OwnerUserId == null && link.OwnerGroupId == null, cancellationToken);

        if (!hasUnownedCategories && !hasUnownedLinks)
        {
            return;
        }

        if (hasUnownedCategories)
        {
            var ownerUserId = await ResolveLegacyOwnerUserIdAsync(cancellationToken);
            if (ownerUserId == Guid.Empty)
            {
                return;
            }

            await dbContext.Categories
                .Where(category => category.OwnerUserId == null && category.OwnerGroupId == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(category => category.OwnerType, OwnerType.User)
                        .SetProperty(category => category.OwnerUserId, ownerUserId),
                    cancellationToken);
        }

        if (hasUnownedLinks)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                UPDATE meal_links AS l
                SET "OwnerType" = c."OwnerType",
                    "OwnerUserId" = c."OwnerUserId",
                    "OwnerGroupId" = c."OwnerGroupId"
                FROM meal_categories AS c
                WHERE l."CategoryId" = c."Id"
                  AND l."OwnerUserId" IS NULL
                  AND l."OwnerGroupId" IS NULL
                """,
                cancellationToken);
        }
    }

    private async Task<Guid> ResolveLegacyOwnerUserIdAsync(CancellationToken cancellationToken)
    {
        var defaultUserId = await dbContext.Users
            .Where(user => user.Id == WellKnownUsers.DefaultUserId)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultUserId != Guid.Empty)
        {
            return defaultUserId;
        }

        return await dbContext.Users
            .OrderBy(user => user.CreatedAtUtc)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
