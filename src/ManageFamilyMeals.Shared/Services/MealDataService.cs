using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public sealed class MealDataService(IAppDataStore dataStore) : IMealDataService
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

    public IReadOnlyList<MealCategory> GetFavoriteCategories() =>
        MealDataQueries.GetFavoriteCategories(_data);

    public IReadOnlyList<MealCategory> GetActiveCategories() =>
        MealDataQueries.GetActiveCategories(_data);

    public IReadOnlyList<MealCategory> GetArchivedCategories() =>
        MealDataQueries.GetArchivedCategories(_data);

    public MealCategory? GetCategory(Guid categoryId) =>
        MealDataQueries.GetCategory(_data, categoryId);

    public MealLink? GetLink(Guid linkId) =>
        MealDataQueries.GetLink(_data, linkId);

    public AppSettings GetSettings() => _data.Settings;

    public bool IsCategoryNameTaken(string name) =>
        MealDataQueries.IsCategoryNameTaken(_data, name);

    public async Task<MealCategory> AddCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();

        if (IsCategoryNameTaken(trimmedName))
        {
            throw new InvalidOperationException("Category name already exists.");
        }

        var category = new MealCategory
        {
            Name = trimmedName
        };

        _data.Categories.Add(category);
        await PersistAsync(cancellationToken);
        return category;
    }

    public async Task<bool> ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = _data.Categories.FirstOrDefault(item => item.Id == categoryId && !item.IsDeleted);
        if (category is null)
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
        if (GetCategory(categoryId) is null)
        {
            throw new KeyNotFoundException($"Category '{categoryId}' was not found.");
        }

        var link = new MealLink
        {
            CategoryId = categoryId,
            TitleEn = titleEn.Trim(),
            TitleAr = titleAr.Trim(),
            Url = url.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
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

        _data.Categories.RemoveAll(category =>
            category.IsDeleted && category.DeletedAtUtc is not null && category.DeletedAtUtc < threshold);

        _data.Links.RemoveAll(link =>
            link.IsDeleted && link.DeletedAtUtc is not null && link.DeletedAtUtc < threshold);
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await dataStore.SaveAsync(_data, cancellationToken);
        DataChanged?.Invoke();
    }
}
