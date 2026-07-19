using ManageFamilyMeals.Shared.Helpers;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Pages;

public partial class Share
{
    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    [Inject]
    private ILinkPreviewClient LinkPreviewClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "url")]
    public string? SharedUrl { get; set; }

    [SupplyParameterFromQuery(Name = "title")]
    public string? SharedTitle { get; set; }

    [SupplyParameterFromQuery(Name = "text")]
    public string? SharedText { get; set; }

    private readonly ShareForm _form = new();
    private readonly CategoryForm _categoryForm = new();
    private IReadOnlyList<MealCategory> _categories = [];
    private string? _error;
    private string? _categoryError;
    private string? _success;
    private bool _isSaving;
    private bool _isReady;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        DataService.DataChanged += RefreshCategories;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await DataService.InitializeAsync();
        ApplySharedPayload();
        RefreshCategories();
        _isReady = true;
        StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        if (_isReady)
        {
            ApplySharedPayload();
        }
    }

    private void RefreshCategories()
    {
        _categories = DataService.GetActiveCategories();

        if (_categories.Count > 0 && string.IsNullOrWhiteSpace(_form.CategoryId))
        {
            _form.CategoryId = _categories[0].Id.ToString();
        }

        StateHasChanged();
    }

    private void ApplySharedPayload()
    {
        var resolvedUrl = SharedLinkParser.ExtractUrl(SharedUrl, SharedText);
        var resolvedTitle = SharedLinkParser.ExtractTitle(SharedTitle, SharedText, resolvedUrl);

        if (!string.IsNullOrWhiteSpace(resolvedUrl))
        {
            _form.Url = resolvedUrl;
        }

        if (!string.IsNullOrWhiteSpace(resolvedTitle))
        {
            _form.TitleEn = resolvedTitle;
        }
    }

    private async Task CreateCategoryAsync()
    {
        _categoryError = null;

        if (string.IsNullOrWhiteSpace(_categoryForm.Name))
        {
            _categoryError = L["CategoryNameRequired"];
            return;
        }

        if (DataService.IsCategoryNameTaken(_categoryForm.Name))
        {
            _categoryError = L["CategoryNameDuplicate"];
            return;
        }

        var category = await DataService.AddCategoryAsync(_categoryForm.Name);
        _form.CategoryId = category.Id.ToString();
        _categoryForm.Name = string.Empty;
        RefreshCategories();
    }

    private async Task SaveLinkAsync()
    {
        _error = null;
        _success = null;

        if (!Guid.TryParse(_form.CategoryId, out var categoryId))
        {
            _error = L["SelectCategoryRequired"];
            return;
        }

        if (!Uri.TryCreate(_form.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _error = L["InvalidUrl"];
            return;
        }

        _isSaving = true;

        try
        {
            var link = await DataService.AddLinkAsync(
                categoryId,
                _form.TitleEn,
                _form.TitleAr,
                _form.Url,
                _form.Note);

            var preview = await LinkPreviewClient.FetchAsync(link.Url);
            if (preview is not null)
            {
                await DataService.UpdateLinkPreviewAsync(link.Id, preview);
            }

            _success = L["LinkSaved"];
            NavigationManager.NavigateTo($"/category/{categoryId}");
        }
        finally
        {
            _isSaving = false;
        }
    }

    public override void Dispose()
    {
        DataService.DataChanged -= RefreshCategories;
        base.Dispose();
    }

    private sealed class ShareForm
    {
        public string CategoryId { get; set; } = string.Empty;

        public string TitleEn { get; set; } = string.Empty;

        public string TitleAr { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
    }

    private sealed class CategoryForm
    {
        public string Name { get; set; } = string.Empty;
    }
}
