using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Pages;

public partial class Home : IDisposable
{
    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    private readonly CategoryForm _form = new();
    private string? _error;
    private string _searchTerm = string.Empty;
    private IReadOnlyList<MealCategory> _favoriteCategories = [];
    private IReadOnlyList<MealCategory> _allCategories = [];
    private bool _hasAnyCategories;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        DataService.DataChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        _hasAnyCategories = DataService.GetActiveCategories().Count > 0;
        _favoriteCategories = DataService.GetFavoriteCategories()
            .Where(category => MatchesSearch(category.Name))
            .ToList();
        _allCategories = DataService.GetActiveCategories()
            .Where(category => MatchesSearch(category.Name))
            .ToList();
        StateHasChanged();
    }

    private void OnSearchChanged()
    {
        Refresh();
    }

    private bool MatchesSearch(string categoryName) =>
        string.IsNullOrWhiteSpace(_searchTerm)
        || categoryName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase);

    private async Task CreateCategoryAsync()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_form.Name))
        {
            _error = L["CategoryNameRequired"];
            return;
        }

        if (DataService.IsCategoryNameTaken(_form.Name))
        {
            _error = L["CategoryNameDuplicate"];
            return;
        }

        await DataService.AddCategoryAsync(_form.Name);
        _form.Name = string.Empty;
    }

    public new void Dispose()
    {
        DataService.DataChanged -= Refresh;
        base.Dispose();
    }

    private sealed class CategoryForm
    {
        public string Name { get; set; } = string.Empty;
    }
}
