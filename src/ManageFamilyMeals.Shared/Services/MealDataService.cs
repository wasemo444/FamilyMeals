using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public sealed class MealDataService(
    IAppDataStore dataStore,
    ICurrentUserContext currentUser,
    IOwnershipAuthorization ownershipAuthorization) : IMealDataService
{
    private AppData _data = new();
    private bool _initialized;

    public event Action? DataChanged;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        EnsureLoadedAsync(cancellationToken);

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _data = await dataStore.LoadAsync(cancellationToken) ?? new AppData();
        _initialized = true;
    }

    public async Task RunMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        MigrateLegacyData();
        PurgeExpiredArchive();
        await PersistAsync(cancellationToken);
    }

    public AppData GetSnapshot() => new()
    {
        Categories = _data.Categories.ToList(),
        Links = _data.Links.ToList(),
        Settings = new AppSettings { CultureCode = _data.Settings.CultureCode }
    };

    public void ApplySettings(AppSettings settings)
    {
        _data.Settings = new AppSettings { CultureCode = settings.CultureCode };
        DataChanged?.Invoke();
    }

    public IReadOnlyList<MealCategory> GetFavoriteCategories(HomeContentFilter filter = HomeContentFilter.All) =>
        MealDataQueries.GetFavoriteCategories(_data, filter);

    public IReadOnlyList<MealCategory> GetActiveCategories(HomeContentFilter filter = HomeContentFilter.All) =>
        MealDataQueries.GetActiveCategories(_data, filter);

    public IReadOnlyList<MealCategory> GetArchivedCategories() =>
        MealDataQueries.GetArchivedCategories(_data);

    public MealCategory? GetCategory(Guid categoryId) =>
        MealDataQueries.GetCategory(_data, categoryId);

    public MealLink? GetLink(Guid linkId) =>
        MealDataQueries.GetLink(_data, linkId);

    public AppSettings GetSettings() => _data.Settings;

    public bool IsCategoryNameTaken(string name, ContentOwner owner) =>
        MealDataQueries.IsCategoryNameTaken(_data, name, owner);

    public async Task<MealCategory> AddCategoryAsync(
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
        var category = new MealCategory
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

    public IReadOnlyList<MealLink> GetFavoriteLinks(Guid categoryId) =>
        MealDataQueries.GetFavoriteLinks(_data, categoryId);

    public IReadOnlyList<MealLink> GetActiveLinks(Guid categoryId) =>
        MealDataQueries.GetActiveLinks(_data, categoryId);

    public IReadOnlyList<MealLink> GetArchivedLinks(Guid categoryId) =>
        MealDataQueries.GetArchivedLinks(_data, categoryId);

    public IReadOnlyList<MealLink> GetAllArchivedLinks() =>
        MealDataQueries.GetAllArchivedLinks(_data);

    public async Task<MealLink> AddLinkAsync(
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
        var link = new MealLink
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
