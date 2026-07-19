using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public sealed class MealDataService(IAppDataStore dataStore) : IMealDataService
{
    private AppData _data = new();
    private bool _initialized;

    public event Action? DataChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _data = await dataStore.LoadAsync(cancellationToken) ?? new AppData();
        MigrateLegacyData();
        PurgeExpiredArchive();
        await PersistAsync(cancellationToken);
        _initialized = true;
    }

    public IReadOnlyList<MealCategory> GetFavoriteCategories() =>
        SortCategories(_data.Categories.Where(category => !category.IsDeleted && category.IsFavorite));

    public IReadOnlyList<MealCategory> GetActiveCategories() =>
        SortCategories(_data.Categories.Where(category => !category.IsDeleted));

    public IReadOnlyList<MealCategory> GetArchivedCategories() =>
        _data.Categories
            .Where(category => category.IsDeleted)
            .OrderByDescending(category => category.DeletedAtUtc)
            .ToList();

    public MealCategory? GetCategory(Guid categoryId) =>
        _data.Categories.FirstOrDefault(category => category.Id == categoryId && !category.IsDeleted);

    public bool IsCategoryNameTaken(string name) =>
        _data.Categories.Any(category =>
            !category.IsDeleted
            && category.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

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

    public async Task ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = _data.Categories.FirstOrDefault(item => item.Id == categoryId && !item.IsDeleted);
        if (category is null)
        {
            return;
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
    }

    public async Task RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = _data.Categories.FirstOrDefault(item => item.Id == categoryId && item.IsDeleted);
        if (category is null)
        {
            return;
        }

        category.IsDeleted = false;
        category.DeletedAtUtc = null;

        foreach (var link in _data.Links.Where(link => link.CategoryId == categoryId && link.IsDeleted))
        {
            link.IsDeleted = false;
            link.DeletedAtUtc = null;
        }

        await PersistAsync(cancellationToken);
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
        SortLinks(_data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted && link.IsFavorite));

    public IReadOnlyList<MealLink> GetActiveLinks(Guid categoryId) =>
        SortLinks(_data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted));

    public IReadOnlyList<MealLink> GetArchivedLinks(Guid categoryId) =>
        _data.Links
            .Where(link => link.CategoryId == categoryId && link.IsDeleted)
            .OrderByDescending(link => link.DeletedAtUtc)
            .ToList();

    public IReadOnlyList<MealLink> GetAllArchivedLinks() =>
        _data.Links
            .Where(link => link.IsDeleted)
            .OrderByDescending(link => link.DeletedAtUtc)
            .ToList();

    public async Task<MealLink> AddLinkAsync(
        Guid categoryId,
        string titleEn,
        string titleAr,
        string url,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
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

    public async Task ArchiveLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = _data.Links.FirstOrDefault(item => item.Id == linkId && !item.IsDeleted);
        if (link is null)
        {
            return;
        }

        link.IsDeleted = true;
        link.DeletedAtUtc = DateTime.UtcNow;
        await PersistAsync(cancellationToken);
    }

    public async Task RestoreLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = _data.Links.FirstOrDefault(item => item.Id == linkId && item.IsDeleted);
        if (link is null)
        {
            return;
        }

        link.IsDeleted = false;
        link.DeletedAtUtc = null;
        await PersistAsync(cancellationToken);
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

    private static IReadOnlyList<MealCategory> SortCategories(IEnumerable<MealCategory> categories) =>
        categories
            .OrderByDescending(category => category.IsFavorite)
            .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<MealLink> SortLinks(IEnumerable<MealLink> links) =>
        links
            .OrderByDescending(link => link.IsFavorite)
            .ThenByDescending(link => link.CreatedAtUtc)
            .ToList();

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await dataStore.SaveAsync(_data, cancellationToken);
        DataChanged?.Invoke();
    }
}
