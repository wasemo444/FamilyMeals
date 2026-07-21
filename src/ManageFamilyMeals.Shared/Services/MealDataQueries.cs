using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public static class MealDataQueries
{
    public static IReadOnlyList<MealCategory> GetFavoriteCategories(AppData data) =>
        SortCategories(data.Categories.Where(category => !category.IsDeleted && category.IsFavorite));

    public static IReadOnlyList<MealCategory> GetActiveCategories(AppData data) =>
        SortCategories(data.Categories.Where(category => !category.IsDeleted));

    public static IReadOnlyList<MealCategory> GetArchivedCategories(AppData data) =>
        data.Categories
            .Where(category => category.IsDeleted)
            .OrderByDescending(category => category.DeletedAtUtc)
            .ToList();

    public static MealCategory? GetCategory(AppData data, Guid categoryId) =>
        data.Categories.FirstOrDefault(category => category.Id == categoryId && !category.IsDeleted);

    public static bool IsCategoryNameTaken(AppData data, string name) =>
        data.Categories.Any(category =>
            !category.IsDeleted
            && category.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<MealLink> GetFavoriteLinks(AppData data, Guid categoryId) =>
        SortLinks(data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted && link.IsFavorite));

    public static IReadOnlyList<MealLink> GetActiveLinks(AppData data, Guid categoryId) =>
        SortLinks(data.Links.Where(link => link.CategoryId == categoryId && !link.IsDeleted));

    public static IReadOnlyList<MealLink> GetArchivedLinks(AppData data, Guid categoryId) =>
        data.Links
            .Where(link => link.CategoryId == categoryId && link.IsDeleted)
            .OrderByDescending(link => link.DeletedAtUtc)
            .ToList();

    public static IReadOnlyList<MealLink> GetAllArchivedLinks(AppData data) =>
        data.Links
            .Where(link => link.IsDeleted)
            .OrderByDescending(link => link.DeletedAtUtc)
            .ToList();

    public static MealLink? GetLink(AppData data, Guid linkId) =>
        data.Links.FirstOrDefault(link => link.Id == linkId);

    public static IReadOnlyList<MealCategory> SortCategories(IEnumerable<MealCategory> categories) =>
        categories
            .OrderByDescending(category => category.IsFavorite)
            .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static IReadOnlyList<MealLink> SortLinks(IEnumerable<MealLink> links) =>
        links
            .OrderByDescending(link => link.IsFavorite)
            .ThenByDescending(link => link.CreatedAtUtc)
            .ToList();
}
