using System.Net;
using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public sealed class ApiMealDataService(IHttpClientFactory httpClientFactory) : IMealDataService
{
    private AppData _cache = new();
    private bool _initialized;

    public event Action? DataChanged;

    public event Func<Task>? Unauthorized;

    private HttpClient Http => httpClientFactory.CreateClient("MealDataApi");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        var response = await Http.GetAsync("/api/bootstrap", cancellationToken);
        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        _cache = await response.Content.ReadFromJsonAsync<AppData>(cancellationToken) ?? new AppData();
        _initialized = true;
        NotifyChanged();
    }

    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) =>
        InitializeAsync(cancellationToken);

    public Task RunMaintenanceAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public AppData GetSnapshot() => new()
    {
        Categories = _cache.Categories.ToList(),
        Links = _cache.Links.ToList(),
        Settings = new AppSettings { CultureCode = _cache.Settings.CultureCode }
    };

    public void ApplySettings(AppSettings settings)
    {
        _cache.Settings = new AppSettings { CultureCode = settings.CultureCode };
        NotifyChanged();
    }

    public IReadOnlyList<MealCategory> GetFavoriteCategories() =>
        MealDataQueries.GetFavoriteCategories(_cache);

    public IReadOnlyList<MealCategory> GetActiveCategories() =>
        MealDataQueries.GetActiveCategories(_cache);

    public IReadOnlyList<MealCategory> GetArchivedCategories() =>
        MealDataQueries.GetArchivedCategories(_cache);

    public MealCategory? GetCategory(Guid categoryId) =>
        MealDataQueries.GetCategory(_cache, categoryId);

    public MealLink? GetLink(Guid linkId) =>
        MealDataQueries.GetLink(_cache, linkId);

    public AppSettings GetSettings() => _cache.Settings;

    public bool IsCategoryNameTaken(string name) =>
        MealDataQueries.IsCategoryNameTaken(_cache, name);

    public async Task<MealCategory> AddCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/categories", new { name }, cancellationToken);
        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<MealCategory>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created category.");

        await ReloadFromServerAsync(cancellationToken);
        return category;
    }

    public async Task<bool> ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/categories/{categoryId}/archive", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/categories/{categoryId}/restore", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    public async Task ToggleCategoryFavoriteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/categories/{categoryId}/favorite", null, cancellationToken);
        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
    }

    public IReadOnlyList<MealLink> GetFavoriteLinks(Guid categoryId) =>
        MealDataQueries.GetFavoriteLinks(_cache, categoryId);

    public IReadOnlyList<MealLink> GetActiveLinks(Guid categoryId) =>
        MealDataQueries.GetActiveLinks(_cache, categoryId);

    public IReadOnlyList<MealLink> GetArchivedLinks(Guid categoryId) =>
        MealDataQueries.GetArchivedLinks(_cache, categoryId);

    public IReadOnlyList<MealLink> GetAllArchivedLinks() =>
        MealDataQueries.GetAllArchivedLinks(_cache);

    public async Task<MealLink> AddLinkAsync(
        Guid categoryId,
        string titleEn,
        string titleAr,
        string url,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync(
            $"/api/categories/{categoryId}/links",
            new { titleEn, titleAr, url, note },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Category '{categoryId}' was not found.");
        }

        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        var link = await response.Content.ReadFromJsonAsync<MealLink>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created link.");

        await ReloadFromServerAsync(cancellationToken);
        return link;
    }

    public async Task<bool> ArchiveLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/links/{linkId}/archive", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/links/{linkId}/restore", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    public async Task ToggleLinkFavoriteAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/links/{linkId}/favorite", null, cancellationToken);
        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
    }

    public async Task UpdateLinkPreviewAsync(Guid linkId, LinkPreviewData preview, CancellationToken cancellationToken = default)
    {
        var response = await Http.PutAsJsonAsync($"/api/links/{linkId}/preview", preview, cancellationToken);
        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        await ReloadFromServerAsync(cancellationToken);
    }

    private async Task ReloadFromServerAsync(CancellationToken cancellationToken)
    {
        var response = await Http.GetAsync("/api/bootstrap", cancellationToken);
        await EnsureAuthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        _cache = await response.Content.ReadFromJsonAsync<AppData>(cancellationToken) ?? new AppData();
        NotifyChanged();
    }

    private async Task EnsureAuthorizedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (Unauthorized is not null)
            {
                await Unauthorized.Invoke();
            }

            throw new UnauthorizedAccessException("Authentication is required.");
        }

        await Task.CompletedTask;
    }

    private void NotifyChanged() => DataChanged?.Invoke();
}
