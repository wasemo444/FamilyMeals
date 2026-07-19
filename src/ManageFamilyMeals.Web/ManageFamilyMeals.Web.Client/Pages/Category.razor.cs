using ManageFamilyMeals.Shared.Extensions;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Pages;

public partial class Category : IDisposable
{
    [Parameter]
    public Guid CategoryId { get; set; }

    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    [Inject]
    private ILinkPreviewClient LinkPreviewClient { get; set; } = default!;

    private readonly LinkForm _form = new();
    private MealCategory? _category;
    private string _pageTitle = "Manage Family Meals";
    private string? _error;
    private bool _isAdding;
    private bool _previewsBackfillStarted;
    private string _searchTerm = string.Empty;
    private IReadOnlyList<MealLink> _favoriteLinks = [];
    private IReadOnlyList<MealLink> _allLinks = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        DataService.DataChanged += Refresh;
        Refresh();
    }

    protected override void OnParametersSet()
    {
        Refresh();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _previewsBackfillStarted || _category is null)
        {
            return;
        }

        _previewsBackfillStarted = true;
        await BackfillMissingPreviewsAsync();
    }

    private void Refresh()
    {
        _category = DataService.GetCategory(CategoryId);
        _pageTitle = _category?.Name ?? L["CategoryNotFound"];
        _favoriteLinks = DataService.GetFavoriteLinks(CategoryId)
            .Where(link => link.MatchesSearch(_searchTerm))
            .ToList();
        _allLinks = DataService.GetActiveLinks(CategoryId)
            .Where(link => link.MatchesSearch(_searchTerm))
            .ToList();
        StateHasChanged();
    }

    private void OnSearchChanged()
    {
        Refresh();
    }

    private async Task BackfillMissingPreviewsAsync()
    {
        var links = DataService.GetActiveLinks(CategoryId)
            .Where(link => string.IsNullOrWhiteSpace(link.PreviewImageUrl))
            .ToList();

        foreach (var link in links)
        {
            var preview = await LinkPreviewClient.FetchAsync(link.Url);
            if (preview is not null)
            {
                await DataService.UpdateLinkPreviewAsync(link.Id, preview);
            }
        }
    }

    private async Task AddLinkAsync()
    {
        _error = null;

        if (!Uri.TryCreate(_form.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _error = L["InvalidUrl"];
            return;
        }

        _isAdding = true;

        try
        {
            var link = await DataService.AddLinkAsync(
                CategoryId,
                _form.TitleEn,
                _form.TitleAr,
                _form.Url,
                _form.Note);
            var preview = await LinkPreviewClient.FetchAsync(link.Url);
            if (preview is not null)
            {
                await DataService.UpdateLinkPreviewAsync(link.Id, preview);
            }

            _form.TitleEn = string.Empty;
            _form.TitleAr = string.Empty;
            _form.Url = string.Empty;
            _form.Note = string.Empty;
        }
        finally
        {
            _isAdding = false;
        }
    }

    public new void Dispose()
    {
        DataService.DataChanged -= Refresh;
        base.Dispose();
    }

    private sealed class LinkForm
    {
        public string TitleEn { get; set; } = string.Empty;

        public string TitleAr { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
    }
}
