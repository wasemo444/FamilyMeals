using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Components;

/// <summary>
/// Card component displaying a meal category with favorite and archive actions.
/// </summary>
public partial class CategoryCard
{
    [Parameter, EditorRequired]
    public MealCategory Category { get; set; } = default!;

    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    private int LinkCount => DataService.GetActiveLinks(Category.Id).Count;

    private string FavoriteLabel => Category.IsFavorite
        ? L["Unfavorite"]
        : L["Favorite"];

    private string SharedLabel => string.IsNullOrWhiteSpace(Category.OwnerGroupName)
        ? L["Shared"]
        : L.Format("SharedWithGroup", Category.OwnerGroupName);

    private async Task ToggleFavoriteAsync()
    {
        await DataService.ToggleCategoryFavoriteAsync(Category.Id);
    }

    private async Task ArchiveAsync()
    {
        await DataService.ArchiveCategoryAsync(Category.Id);
    }
}
