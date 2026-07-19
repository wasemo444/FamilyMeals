using ManageFamilyMeals.Shared.Extensions;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Pages;

public partial class Archive : IDisposable
{
    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    private IReadOnlyList<MealCategory> _archivedCategories = [];
    private IReadOnlyList<MealLink> _archivedLinks = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        DataService.DataChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        _archivedCategories = DataService.GetArchivedCategories();
        _archivedLinks = DataService.GetAllArchivedLinks();
        StateHasChanged();
    }

    private string GetLinkLabel(MealLink link) =>
        link.GetLocalizedTitle(CultureService.CurrentCulture);

    private async Task RestoreCategoryAsync(Guid categoryId)
    {
        await DataService.RestoreCategoryAsync(categoryId);
    }

    private async Task RestoreLinkAsync(Guid linkId)
    {
        await DataService.RestoreLinkAsync(linkId);
    }

    public new void Dispose()
    {
        DataService.DataChanged -= Refresh;
        base.Dispose();
    }
}
