using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public interface IMealDataService
{
    event Action? DataChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<MealCategory> GetFavoriteCategories();

    IReadOnlyList<MealCategory> GetActiveCategories();

    IReadOnlyList<MealCategory> GetArchivedCategories();

    MealCategory? GetCategory(Guid categoryId);

    bool IsCategoryNameTaken(string name);

    Task<MealCategory> AddCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task ToggleCategoryFavoriteAsync(Guid categoryId, CancellationToken cancellationToken = default);

    IReadOnlyList<MealLink> GetFavoriteLinks(Guid categoryId);

    IReadOnlyList<MealLink> GetActiveLinks(Guid categoryId);

    IReadOnlyList<MealLink> GetArchivedLinks(Guid categoryId);

    IReadOnlyList<MealLink> GetAllArchivedLinks();

    Task<MealLink> AddLinkAsync(
        Guid categoryId,
        string titleEn,
        string titleAr,
        string url,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task ArchiveLinkAsync(Guid linkId, CancellationToken cancellationToken = default);

    Task RestoreLinkAsync(Guid linkId, CancellationToken cancellationToken = default);

    Task ToggleLinkFavoriteAsync(Guid linkId, CancellationToken cancellationToken = default);

    Task UpdateLinkPreviewAsync(Guid linkId, LinkPreviewData preview, CancellationToken cancellationToken = default);
}
