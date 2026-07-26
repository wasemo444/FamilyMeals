using LinkNest.Shared.Extensions;
using LinkNest.Shared.Models;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace LinkNest.Web.Client.Pages;

/// <summary>
/// Archive page for viewing and restoring archived categories and links.
/// </summary>
/// <remarks>
/// Rendered in interactive WebAssembly mode. Link titles are localized using the active culture.
/// </remarks>
public partial class Archive : IDisposable
{
    [Inject]
    private IContentDataService DataService { get; set; } = default!;

    private IReadOnlyList<ContentCategory> _archivedCategories = [];
    private IReadOnlyList<SavedLink> _archivedLinks = [];

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

    private string GetLinkLabel(SavedLink link) =>
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
