using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

/// <summary>
/// Pure query and sorting helpers over an in-memory <see cref="AppData"/> snapshot.
/// Shared by local and API-backed meal data services to keep filtering logic consistent.
/// </summary>
public static class MealDataQueries
{
    /// <summary>
    /// Returns non-archived favorite categories matching the ownership filter, sorted for display.
    /// </summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="filter">Personal, shared, or all categories.</param>
    public static IReadOnlyList<MealCategory> GetFavoriteCategories(AppData data, HomeContentFilter filter = HomeContentFilter.All) =>
        SortCategories(FilterCategories(data.Categories.Where(category => !category.IsDeleted && category.IsFavorite), filter));

    /// <summary>
    /// Returns non-archived categories matching the ownership filter, sorted for display.
    /// </summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="filter">Personal, shared, or all categories.</param>
    public static IReadOnlyList<MealCategory> GetActiveCategories(AppData data, HomeContentFilter filter = HomeContentFilter.All) =>
        SortCategories(FilterCategories(data.Categories.Where(category => !category.IsDeleted), filter));

    /// <summary>
    /// Returns archived categories ordered by most recently deleted first.
    /// </summary>
    /// <param name="data">Data snapshot to query.</param>
    public static IReadOnlyList<MealCategory> GetArchivedCategories(AppData data) =>
        data.Categories
            .Where(category => category.IsDeleted)
            .OrderByDescending(category => category.DeletedAtUtc)
            .ToList();

    /// <summary>
    /// Finds an active category by id.
    /// </summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="categoryId">Category identifier.</param>
    /// <returns>The category, or <see langword="null"/> when not found or archived.</returns>
    public static MealCategory? GetCategory(AppData data, Guid categoryId) =>
        data.Categories.FirstOrDefault(category => category.Id == categoryId && !category.IsDeleted);

    /// <summary>
    /// Checks name uniqueness within an ownership scope using case-insensitive comparison.
    /// </summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="name">Proposed category name.</param>
    /// <param name="owner">Ownership scope for comparison.</param>
    public static bool IsCategoryNameTaken(AppData data, string name, ContentOwner owner) =>
        data.Categories.Any(category =>
            !category.IsDeleted
            && category.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)
            && category.OwnerType == owner.OwnerType
            && category.OwnerGroupId == owner.OwnerGroupId);

    /// <summary>Returns favorite active links in a category, sorted for display.</summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="categoryId">Parent category id.</param>
    public static IReadOnlyList<MealLink> GetFavoriteLinks(AppData data, Guid categoryId) =>
        SortLinks(data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted && link.IsFavorite));

    /// <summary>Returns active links in a category, sorted for display.</summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="categoryId">Parent category id.</param>
    public static IReadOnlyList<MealLink> GetActiveLinks(AppData data, Guid categoryId) =>
        SortLinks(data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted));

    /// <summary>Returns archived links in a category, most recently deleted first.</summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="categoryId">Parent category id.</param>
    public static IReadOnlyList<MealLink> GetArchivedLinks(AppData data, Guid categoryId) =>
        data.Links
            .Where(link => link.CategoryId == categoryId && link.IsDeleted)
            .OrderByDescending(link => link.DeletedAtUtc)
            .ToList();

    /// <summary>Returns all archived links across categories, most recently deleted first.</summary>
    /// <param name="data">Data snapshot to query.</param>
    public static IReadOnlyList<MealLink> GetAllArchivedLinks(AppData data) =>
        data.Links
            .Where(link => link.IsDeleted)
            .OrderByDescending(link => link.DeletedAtUtc)
            .ToList();

    /// <summary>Finds a link by id regardless of archive state.</summary>
    /// <param name="data">Data snapshot to query.</param>
    /// <param name="linkId">Link identifier.</param>
    public static MealLink? GetLink(AppData data, Guid linkId) =>
        data.Links.FirstOrDefault(link => link.Id == linkId);

    /// <summary>
    /// Sorts categories with favorites first, then alphabetically by name.
    /// </summary>
    /// <param name="categories">Categories to sort.</param>
    public static IReadOnlyList<MealCategory> SortCategories(IEnumerable<MealCategory> categories) =>
        categories
            .OrderByDescending(category => category.IsFavorite)
            .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// Sorts links with favorites first, then by creation time descending.
    /// </summary>
    /// <param name="links">Links to sort.</param>
    public static IReadOnlyList<MealLink> SortLinks(IEnumerable<MealLink> links) =>
        links
            .OrderByDescending(link => link.IsFavorite)
            .ThenByDescending(link => link.CreatedAtUtc)
            .ToList();

    private static IEnumerable<MealCategory> FilterCategories(IEnumerable<MealCategory> categories, HomeContentFilter filter) =>
        categories.Where(category => OwnershipRules.MatchesFilter(category, filter));
}
