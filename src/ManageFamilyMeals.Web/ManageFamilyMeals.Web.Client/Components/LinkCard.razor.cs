using ManageFamilyMeals.Shared.Extensions;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using ManageFamilyMeals.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Components;

public partial class LinkCard
{
    [Parameter, EditorRequired]
    public MealLink Link { get; set; } = default!;

    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    private string DisplayTitle => Link.GetLocalizedTitle(CultureService.CurrentCulture);

    private string? PreviewImageSource => string.IsNullOrWhiteSpace(Link.PreviewImageUrl)
        ? null
        : $"/api/link-preview/image?url={Uri.EscapeDataString(Link.PreviewImageUrl)}";

    private string FavoriteLabel => Link.IsFavorite
        ? L["Unfavorite"]
        : L["Favorite"];

    private async Task ToggleFavoriteAsync()
    {
        await DataService.ToggleLinkFavoriteAsync(Link.Id);
    }

    private async Task ArchiveAsync()
    {
        await DataService.ArchiveLinkAsync(Link.Id);
    }
}
