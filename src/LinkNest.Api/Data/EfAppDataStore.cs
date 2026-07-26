using LinkNest.Api.Data;

using LinkNest.Api.Data.Configurations;

using LinkNest.Api.Data.Entities;

using LinkNest.Api.Mapping;

using LinkNest.Api.Services;

using LinkNest.Shared.Models;

using LinkNest.Shared.Services;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.ChangeTracking;



namespace LinkNest.Api.Data;



/// <summary>
/// EF Core implementation of <see cref="IAppDataStore"/> that loads and persists meal data with ownership filtering.
/// </summary>
/// <remarks>
/// <para>Load and save operations scope categories and links to the current user and their group memberships.</para>
/// <para>Updates require a client <c>RowVersion</c>; missing or stale tokens throw <see cref="ConcurrencyConflictException"/> (HTTP 409).</para>
/// <para>Entities the caller cannot mutate are skipped on delete/update rather than throwing.</para>
/// </remarks>
public sealed class EfAppDataStore(

    AppDbContext dbContext,

    ICurrentUserContext currentUser,

    IOwnershipAuthorization ownershipAuthorization) : IAppDataStore

{

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the HTTP context has no authenticated user.</exception>
    public async Task<AppData?> LoadAsync(CancellationToken cancellationToken = default)

    {

        var userId = currentUser.GetRequiredUserId();

        var memberGroupIds = await ownershipAuthorization.GetMemberGroupIdsAsync(cancellationToken);



        var categories = await dbContext.Categories

            .AsNoTracking()

            .Where(category =>

                (category.OwnerType == OwnerType.User && category.OwnerUserId == userId)

                || (category.OwnerType == OwnerType.Group

                    && category.OwnerGroupId != null

                    && memberGroupIds.Contains(category.OwnerGroupId.Value)))

            .ToListAsync(cancellationToken);



        var categoryIds = categories.Select(category => category.Id).ToHashSet();



        var links = await dbContext.Links

            .AsNoTracking()

            .Where(link =>

                categoryIds.Contains(link.CategoryId)

                && ((link.OwnerType == OwnerType.User && link.OwnerUserId == userId)

                    || (link.OwnerType == OwnerType.Group

                        && link.OwnerGroupId != null

                        && memberGroupIds.Contains(link.OwnerGroupId.Value))))

            .ToListAsync(cancellationToken);



        var groupNames = await dbContext.Groups

            .AsNoTracking()

            .Where(group => memberGroupIds.Contains(group.Id))

            .ToDictionaryAsync(group => group.Id, group => group.Name, cancellationToken);



        var settings = await dbContext.AppSettings

            .AsNoTracking()

            .FirstOrDefaultAsync(item => item.Id == AppSettingsEntityConfiguration.SingletonId, cancellationToken);



        return new AppData

        {

            Categories = categories.Select(category => category.ToModel(groupNames)).ToList(),

            Links = links.Select(link => link.ToModel()).ToList(),

            Settings = settings?.ToModel() ?? new AppSettings()

        };

    }



    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the HTTP context has no authenticated user.</exception>
    /// <exception cref="ConcurrencyConflictException">Thrown when a row version is missing or stale.</exception>
    public async Task SaveAsync(AppData data, CancellationToken cancellationToken = default)

    {

        var userId = currentUser.GetRequiredUserId();

        var memberGroupIds = await ownershipAuthorization.GetMemberGroupIdsAsync(cancellationToken);



        var supportsTransactions = dbContext.Database.IsRelational()

            && dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";



        await using var transaction = supportsTransactions

            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)

            : null;



        var existingCategoryIds = await dbContext.Categories

            .Where(category =>

                (category.OwnerType == OwnerType.User && category.OwnerUserId == userId)

                || (category.OwnerType == OwnerType.Group

                    && category.OwnerGroupId != null

                    && memberGroupIds.Contains(category.OwnerGroupId.Value)))

            .Select(category => category.Id)

            .ToListAsync(cancellationToken);



        var existingLinkIds = await dbContext.Links

            .Where(link =>

                (link.OwnerType == OwnerType.User && link.OwnerUserId == userId)

                || (link.OwnerType == OwnerType.Group

                    && link.OwnerGroupId != null

                    && memberGroupIds.Contains(link.OwnerGroupId.Value)))

            .Select(link => link.Id)

            .ToListAsync(cancellationToken);



        var incomingCategoryIds = data.Categories.Select(category => category.Id).ToHashSet();

        var incomingLinkIds = data.Links.Select(link => link.Id).ToHashSet();



        foreach (var linkId in existingLinkIds.Where(id => !incomingLinkIds.Contains(id)))

        {

            var entity = await dbContext.Links.FindAsync([linkId], cancellationToken);

            if (entity is not null && CanMutateEntity(entity, userId, memberGroupIds))

            {

                dbContext.Links.Remove(entity);

            }

        }



        foreach (var categoryId in existingCategoryIds.Where(id => !incomingCategoryIds.Contains(id)))

        {

            var entity = await dbContext.Categories.FindAsync([categoryId], cancellationToken);

            if (entity is not null && CanMutateEntity(entity, userId, memberGroupIds))

            {

                dbContext.Categories.Remove(entity);

            }

        }



        foreach (var category in data.Categories)

        {

            var entity = await dbContext.Categories.FindAsync([category.Id], cancellationToken);

            if (entity is null)

            {

                OwnershipRules.ValidateCreateOwner(

                    new ContentOwner(category.OwnerType, category.OwnerGroupId),

                    userId,

                    memberGroupIds);



                var newEntity = category.ToEntity();

                ApplyOwnership(newEntity, category, userId);

                dbContext.Categories.Add(newEntity);

            }

            else if (CanMutateEntity(entity, userId, memberGroupIds))

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

                var category = data.Categories.FirstOrDefault(item => item.Id == link.CategoryId);

                if (category is null)

                {

                    continue;

                }



                OwnershipRules.ValidateCreateOwner(

                    new ContentOwner(category.OwnerType, category.OwnerGroupId),

                    userId,

                    memberGroupIds);



                var newEntity = link.ToEntity();

                ApplyOwnership(newEntity, category, userId);

                dbContext.Links.Add(newEntity);

            }

            else if (CanMutateEntity(entity, userId, memberGroupIds))

            {

                var previousCategoryId = entity.CategoryId;

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

                if (previousCategoryId != link.CategoryId)

                {

                    var category = data.Categories.FirstOrDefault(item => item.Id == link.CategoryId);

                    if (category is not null)

                    {

                        ApplyOwnership(entity, category, userId);

                    }

                }

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



    private static bool CanMutateEntity(ContentCategoryEntity entity, Guid userId, IReadOnlySet<Guid> memberGroupIds) =>

        OwnershipRules.CanMutate(entity.OwnerType, entity.OwnerUserId, entity.OwnerGroupId, userId, memberGroupIds);



    private static bool CanMutateEntity(SavedLinkEntity entity, Guid userId, IReadOnlySet<Guid> memberGroupIds) =>

        OwnershipRules.CanMutate(entity.OwnerType, entity.OwnerUserId, entity.OwnerGroupId, userId, memberGroupIds);



    private static void ApplyOwnership(ContentCategoryEntity entity, ContentCategory model, Guid userId)

    {

        if (model.OwnerType == OwnerType.Group && model.OwnerGroupId is not null)

        {

            entity.OwnerType = OwnerType.Group;

            entity.OwnerGroupId = model.OwnerGroupId;

            entity.OwnerUserId = null;

            return;

        }



        StampUserOwnership(entity, userId);

    }



    private static void ApplyOwnership(SavedLinkEntity entity, ContentCategory category, Guid userId)

    {

        if (category.OwnerType == OwnerType.Group && category.OwnerGroupId is not null)

        {

            entity.OwnerType = OwnerType.Group;

            entity.OwnerGroupId = category.OwnerGroupId;

            entity.OwnerUserId = null;

            return;

        }



        StampUserOwnership(entity, userId);

    }



    private static void StampUserOwnership(ContentCategoryEntity entity, Guid userId)

    {

        entity.OwnerType = OwnerType.User;

        entity.OwnerUserId = userId;

        entity.OwnerGroupId = null;

    }



    private static void StampUserOwnership(SavedLinkEntity entity, Guid userId)

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


