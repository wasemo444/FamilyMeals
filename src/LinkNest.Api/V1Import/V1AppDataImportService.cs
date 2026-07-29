using System.Text.Json;
using LinkNest.Api.Data;
using LinkNest.Api.Data.Entities;
using LinkNest.Api.Mapping;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.V1Import;

/// <summary>
/// One-time importer that loads v1 exported JSON (or localStorage-shaped payloads) into PostgreSQL.
/// </summary>
/// <remarks>
/// <para>All imported categories and links are assigned <see cref="OwnerType.User"/> ownership for the specified user.</para>
/// <para>
/// The import is idempotent by primary key: re-running against the same export skips rows whose ids already exist
/// in the database (no duplicate categories/links are created). Intended as a run-once cutover tool per user export.
/// </para>
/// </remarks>
public sealed class V1AppDataImportService(AppDbContext dbContext, ILogger<V1AppDataImportService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes and imports a v1 JSON payload for the given user.
    /// </summary>
    /// <param name="json">Exported v1 <see cref="AppData"/> JSON or equivalent localStorage value.</param>
    /// <param name="targetUserId">Identity user id that will own all imported rows.</param>
    /// <param name="cancellationToken">Token used to cancel database operations.</param>
    public async Task<V1AppDataImportResult> ImportFromJsonAsync(
        string json,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new V1AppDataImportResult { InvalidPayload = true };
        }

        AppData? source;
        try
        {
            source = JsonSerializer.Deserialize<AppData>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize v1 import payload.");
            return new V1AppDataImportResult { InvalidPayload = true };
        }

        if (source is null)
        {
            return new V1AppDataImportResult { InvalidPayload = true };
        }

        return await ImportAsync(source, targetUserId, cancellationToken);
    }

    /// <summary>
    /// Imports deserialized v1 data for the given user.
    /// </summary>
    public async Task<V1AppDataImportResult> ImportAsync(
        AppData source,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users
            .AnyAsync(user => user.Id == targetUserId, cancellationToken);

        if (!userExists)
        {
            logger.LogWarning("V1 import aborted: user {UserId} was not found.", targetUserId);
            return new V1AppDataImportResult { UserNotFound = true };
        }

        var existingCategoryOwners = await dbContext.Categories
            .AsNoTracking()
            .Select(category => new { category.Id, category.OwnerUserId })
            .ToDictionaryAsync(category => category.Id, category => category.OwnerUserId, cancellationToken);

        var existingLinkIds = await dbContext.Links
            .AsNoTracking()
            .Select(link => link.Id)
            .ToHashSetAsync(cancellationToken);

        var categoriesImported = 0;
        var categoriesSkipped = 0;
        var importedCategoryIds = new HashSet<Guid>();

        foreach (var category in source.Categories)
        {
            if (existingCategoryOwners.TryGetValue(category.Id, out var existingOwnerId))
            {
                categoriesSkipped++;
                if (existingOwnerId == targetUserId)
                {
                    importedCategoryIds.Add(category.Id);
                }
                else
                {
                    logger.LogWarning(
                        "Skipping v1 category {CategoryId}: already owned by a different user.",
                        category.Id);
                }

                continue;
            }

            var entity = MapCategory(category, targetUserId);
            dbContext.Categories.Add(entity);
            importedCategoryIds.Add(entity.Id);
            categoriesImported++;
        }

        var linksImported = 0;
        var linksSkipped = 0;

        foreach (var link in source.Links)
        {
            if (existingLinkIds.Contains(link.Id))
            {
                linksSkipped++;
                continue;
            }

            if (!importedCategoryIds.Contains(link.CategoryId))
            {
                logger.LogWarning(
                    "Skipping v1 link {LinkId}: parent category {CategoryId} is missing from import payload.",
                    link.Id,
                    link.CategoryId);
                linksSkipped++;
                continue;
            }

            var entity = MapLink(link, targetUserId);
            dbContext.Links.Add(entity);
            linksImported++;
        }

        if (categoriesImported > 0 || linksImported > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "V1 import for user {UserId}: categories imported={CategoriesImported}, skipped={CategoriesSkipped}; links imported={LinksImported}, skipped={LinksSkipped}.",
            targetUserId,
            categoriesImported,
            categoriesSkipped,
            linksImported,
            linksSkipped);

        return new V1AppDataImportResult
        {
            CategoriesImported = categoriesImported,
            CategoriesSkipped = categoriesSkipped,
            LinksImported = linksImported,
            LinksSkipped = linksSkipped
        };
    }

    private static ContentCategoryEntity MapCategory(ContentCategory source, Guid targetUserId)
    {
        var entity = source.ToEntity();
        entity.OwnerType = OwnerType.User;
        entity.OwnerUserId = targetUserId;
        entity.OwnerGroupId = null;
        return entity;
    }

    private static SavedLinkEntity MapLink(SavedLink source, Guid targetUserId)
    {
        var entity = source.ToEntity();

        if (string.IsNullOrWhiteSpace(entity.TitleEn) && !string.IsNullOrWhiteSpace(entity.LegacyTitle))
        {
            entity.TitleEn = entity.LegacyTitle;
        }

        entity.OwnerType = OwnerType.User;
        entity.OwnerUserId = targetUserId;
        entity.OwnerGroupId = null;

        return entity;
    }
}
