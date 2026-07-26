using LinkNest.Shared.Constants;
using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// Local in-process implementation of <see cref="IContentDataService"/> that persists through
/// <see cref="IAppDataStore"/> and enforces ownership via <see cref="IOwnershipAuthorization"/>.
/// </summary>
/// <param name="dataStore">Persistence layer for the full <see cref="AppData"/> aggregate.</param>
/// <param name="currentUser">Source of the authenticated user id for ownership assignment.</param>
/// <param name="ownershipAuthorization">Validates create and mutate permissions before writes.</param>
/// <remarks>
/// Each mutation reloads data from the store after save so optimistic concurrency tokens stay current.
/// Archiving a category cascades to all active links in that category.
/// </remarks>
public sealed class ContentDataService(
    IAppDataStore dataStore,
    ICurrentUserContext currentUser,
    IOwnershipAuthorization ownershipAuthorization) : IContentDataService
{
    private AppData _data = new();
    private bool _initialized;

    /// <inheritdoc />
    public event Action? DataChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        EnsureLoadedAsync(cancellationToken);

    /// <inheritdoc />
    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _data = await dataStore.LoadAsync(cancellationToken) ?? new AppData();
        _initialized = true;
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        _data = await dataStore.LoadAsync(cancellationToken) ?? new AppData();
        DataChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task RunMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        MigrateLegacyData();
        PurgeExpiredArchive();
        await PersistAsync(cancellationToken);
    }

    /// <inheritdoc />
    public AppData GetSnapshot() => new()
    {
        Categories = _data.Categories.ToList(),
        Links = _data.Links.ToList(),
        Settings = new AppSettings { CultureCode = _data.Settings.CultureCode }
    };

    /// <inheritdoc />
    public void ApplySettings(AppSettings settings)
    {
        _data.Settings = new AppSettings { CultureCode = settings.CultureCode };
        DataChanged?.Invoke();
    }

    /// <inheritdoc />
    public IReadOnlyList<ContentCategory> GetFavoriteCategories(HomeContentFilter filter = HomeContentFilter.All) =>
        ContentDataQueries.GetFavoriteCategories(_data, filter);

    /// <inheritdoc />
    public IReadOnlyList<ContentCategory> GetActiveCategories(HomeContentFilter filter = HomeContentFilter.All) =>
        ContentDataQueries.GetActiveCategories(_data, filter);

    /// <inheritdoc />
    public IReadOnlyList<ContentCategory> GetArchivedCategories() =>
        ContentDataQueries.GetArchivedCategories(_data);

    /// <inheritdoc />
    public ContentCategory? GetCategory(Guid categoryId) =>
        ContentDataQueries.GetCategory(_data, categoryId);

    /// <inheritdoc />
    public SavedLink? GetLink(Guid linkId) =>
        ContentDataQueries.GetLink(_data, linkId);

    /// <inheritdoc />
    public AppSettings GetSettings() => _data.Settings;

    /// <inheritdoc />
    public bool IsCategoryNameTaken(string name, ContentOwner owner) =>
        ContentDataQueries.IsCategoryNameTaken(_data, name, owner);

    /// <inheritdoc />
    public async Task<ContentCategory> AddCategoryAsync(
        string name,
        ContentOwner owner,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();

        if (IsCategoryNameTaken(trimmedName, owner))
        {
            throw new InvalidOperationException("Category name already exists.");
        }

        await ownershipAuthorization.ValidateCreateOwnerAsync(owner, cancellationToken);

        var userId = currentUser.GetRequiredUserId();
        var category = new ContentCategory
        {
            Name = trimmedName,
            OwnerType = owner.OwnerType,
            OwnerUserId = owner.OwnerType == OwnerType.User ? userId : null,
            OwnerGroupId = owner.OwnerGroupId
        };

        _data.Categories.Add(category);
        await PersistAsync(cancellationToken);
        return GetCategory(category.Id) ?? category;
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = _data.Categories.FirstOrDefault(item => item.Id == categoryId && !item.IsDeleted);
        if (category is null)
        {
            return false;
        }

        try
        {
            await ownershipAuthorization.EnsureCanMutateCategoryAsync(category, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        var deletedAt = DateTime.UtcNow;
        category.IsDeleted = true;
        category.DeletedAtUtc = deletedAt;

        foreach (var link in _data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted))
        {
            link.IsDeleted = true;
            link.DeletedAtUtc = deletedAt;
        }

        await PersistAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = _data.Categories.FirstOrDefault(item => item.Id == categoryId && item.IsDeleted);
        if (category is null)
        {
            return false;
        }

        try
        {
            await ownershipAuthorization.EnsureCanMutateCategoryAsync(category, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        category.IsDeleted = false;
        category.DeletedAtUtc = null;

        foreach (var link in _data.Links.Where(link => link.CategoryId == categoryId && link.IsDeleted))
        {
            link.IsDeleted = false;
            link.DeletedAtUtc = null;
        }

        await PersistAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task ToggleCategoryFavoriteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = _data.Categories.FirstOrDefault(item => item.Id == categoryId && !item.IsDeleted);
        if (category is null)
        {
            return;
        }

        await ownershipAuthorization.EnsureCanMutateCategoryAsync(category, cancellationToken);
        category.IsFavorite = !category.IsFavorite;
        await PersistAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetFavoriteLinks(Guid categoryId) =>
        ContentDataQueries.GetFavoriteLinks(_data, categoryId);

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetActiveLinks(Guid categoryId) =>
        ContentDataQueries.GetActiveLinks(_data, categoryId);

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetArchivedLinks(Guid categoryId) =>
        ContentDataQueries.GetArchivedLinks(_data, categoryId);

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetAllArchivedLinks() =>
        ContentDataQueries.GetAllArchivedLinks(_data);

    /// <inheritdoc />
    public async Task<SavedLink> AddLinkAsync(
        Guid categoryId,
        string titleEn,
        string titleAr,
        string url,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var category = GetCategory(categoryId)
            ?? throw new KeyNotFoundException($"Category '{categoryId}' was not found.");

        await ownershipAuthorization.EnsureCanMutateCategoryAsync(category, cancellationToken);

        var userId = currentUser.GetRequiredUserId();
        var link = new SavedLink
        {
            CategoryId = categoryId,
            TitleEn = titleEn.Trim(),
            TitleAr = titleAr.Trim(),
            Url = url.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            OwnerType = category.OwnerType,
            OwnerUserId = category.OwnerType == OwnerType.User ? userId : null,
            OwnerGroupId = category.OwnerGroupId
        };

        _data.Links.Add(link);
        await PersistAsync(cancellationToken);
        return link;
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = _data.Links.FirstOrDefault(item => item.Id == linkId && !item.IsDeleted);
        if (link is null)
        {
            return false;
        }

        try
        {
            await ownershipAuthorization.EnsureCanMutateLinkAsync(link, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        link.IsDeleted = true;
        link.DeletedAtUtc = DateTime.UtcNow;
        await PersistAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = _data.Links.FirstOrDefault(item => item.Id == linkId && item.IsDeleted);
        if (link is null)
        {
            return false;
        }

        try
        {
            await ownershipAuthorization.EnsureCanMutateLinkAsync(link, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        link.IsDeleted = false;
        link.DeletedAtUtc = null;
        await PersistAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task ToggleLinkFavoriteAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = _data.Links.FirstOrDefault(item => item.Id == linkId && !item.IsDeleted);
        if (link is null)
        {
            return;
        }

        await ownershipAuthorization.EnsureCanMutateLinkAsync(link, cancellationToken);
        link.IsFavorite = !link.IsFavorite;
        await PersistAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateLinkPreviewAsync(Guid linkId, LinkPreviewData preview, CancellationToken cancellationToken = default)
    {
        var link = _data.Links.FirstOrDefault(item => item.Id == linkId && !item.IsDeleted);
        if (link is null)
        {
            return;
        }

        await ownershipAuthorization.EnsureCanMutateLinkAsync(link, cancellationToken);

        link.PreviewTitle = preview.Title;
        link.PreviewDescription = preview.Description;
        link.PreviewImageUrl = preview.ImageUrl;
        link.PreviewSiteName = preview.SiteName;

        if (string.IsNullOrWhiteSpace(link.TitleEn) && !string.IsNullOrWhiteSpace(preview.Title))
        {
            link.TitleEn = preview.Title;
        }

        await PersistAsync(cancellationToken);
    }

    private void MigrateLegacyData()
    {
        foreach (var link in _data.Links)
        {
            if (!string.IsNullOrWhiteSpace(link.LegacyTitle) && string.IsNullOrWhiteSpace(link.TitleEn))
            {
                link.TitleEn = link.LegacyTitle;
                link.LegacyTitle = null;
            }
        }
    }

    private void PurgeExpiredArchive()
    {
        var threshold = ArchivePolicy.ExpirationThresholdUtc;

        var expiredCategoryIds = _data.Categories
            .Where(category => category.IsDeleted
                && category.DeletedAtUtc is not null
                && category.DeletedAtUtc < threshold)
            .Select(category => category.Id)
            .ToHashSet();

        _data.Links.RemoveAll(link =>
            expiredCategoryIds.Contains(link.CategoryId)
            || (link.IsDeleted && link.DeletedAtUtc is not null && link.DeletedAtUtc < threshold));

        _data.Categories.RemoveAll(category => expiredCategoryIds.Contains(category.Id));
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await dataStore.SaveAsync(_data, cancellationToken);
        _data = await dataStore.LoadAsync(cancellationToken) ?? new AppData();
        DataChanged?.Invoke();
    }
}
