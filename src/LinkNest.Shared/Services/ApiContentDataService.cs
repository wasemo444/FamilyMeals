using System.Net;
using System.Net.Http.Json;
using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// Remote HTTP implementation of <see cref="IContentDataService"/> that caches bootstrap data
/// from the API and reloads after each mutation.
/// </summary>
/// <param name="httpClientFactory">Factory for the named <c>LinkNestApi</c> HTTP client.</param>
/// <remarks>
/// Maps HTTP 401 to <see cref="UnauthorizedAccessException"/> and optionally invokes
/// <see cref="Unauthorized"/>. HTTP 409 becomes <see cref="ConcurrencyConflictException"/>.
/// </remarks>
public sealed class ApiContentDataService(IHttpClientFactory httpClientFactory) : IContentDataService
{
    private AppData _cache = new();
    private bool _initialized;

    /// <inheritdoc />
    public event Action? DataChanged;

    /// <summary>
    /// Optional handler invoked when the API returns 401 Unauthorized, typically to redirect to login.
    /// </summary>
    public event Func<Task>? Unauthorized;

    private HttpClient Http => httpClientFactory.CreateClient("LinkNestApi");

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        var response = await Http.GetAsync("/api/bootstrap", cancellationToken);
        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        _cache = await response.Content.ReadFromJsonAsync<AppData>(cancellationToken) ?? new AppData();
        _initialized = true;
        NotifyChanged();
    }

    /// <inheritdoc />
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) =>
        InitializeAsync(cancellationToken);

    /// <inheritdoc />
    public Task RunMaintenanceAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public AppData GetSnapshot() => new()
    {
        Categories = _cache.Categories.ToList(),
        Links = _cache.Links.ToList(),
        Settings = new AppSettings { CultureCode = _cache.Settings.CultureCode }
    };

    /// <inheritdoc />
    public void ApplySettings(AppSettings settings)
    {
        _cache.Settings = new AppSettings { CultureCode = settings.CultureCode };
        NotifyChanged();
    }

    /// <inheritdoc />
    public IReadOnlyList<ContentCategory> GetFavoriteCategories(HomeContentFilter filter = HomeContentFilter.All) =>
        ContentDataQueries.GetFavoriteCategories(_cache, filter);

    /// <inheritdoc />
    public IReadOnlyList<ContentCategory> GetActiveCategories(HomeContentFilter filter = HomeContentFilter.All) =>
        ContentDataQueries.GetActiveCategories(_cache, filter);

    /// <inheritdoc />
    public IReadOnlyList<ContentCategory> GetArchivedCategories() =>
        ContentDataQueries.GetArchivedCategories(_cache);

    /// <inheritdoc />
    public ContentCategory? GetCategory(Guid categoryId) =>
        ContentDataQueries.GetCategory(_cache, categoryId);

    /// <inheritdoc />
    public SavedLink? GetLink(Guid linkId) =>
        ContentDataQueries.GetLink(_cache, linkId);

    /// <inheritdoc />
    public AppSettings GetSettings() => _cache.Settings;

    /// <inheritdoc />
    public bool IsCategoryNameTaken(string name, ContentOwner owner) =>
        ContentDataQueries.IsCategoryNameTaken(_cache, name, owner);

    /// <inheritdoc />
    public async Task<ContentCategory> AddCategoryAsync(
        string name,
        ContentOwner owner,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            name,
            ownerType = owner.OwnerType == OwnerType.Group ? OwnerType.Group : (OwnerType?)null,
            ownerGroupId = owner.OwnerGroupId
        };

        var response = await Http.PostAsJsonAsync("/api/categories", payload, cancellationToken);
        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        var category = await response.Content.ReadFromJsonAsync<ContentCategory>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created category.");

        await ReloadFromServerAsync(cancellationToken);
        return category;
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/categories/{categoryId}/archive", null, cancellationToken);
        if (IsMissingResource(response.StatusCode))
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/categories/{categoryId}/restore", null, cancellationToken);
        if (IsMissingResource(response.StatusCode))
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task ToggleCategoryFavoriteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/categories/{categoryId}/favorite", null, cancellationToken);
        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetFavoriteLinks(Guid categoryId) =>
        ContentDataQueries.GetFavoriteLinks(_cache, categoryId);

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetActiveLinks(Guid categoryId) =>
        ContentDataQueries.GetActiveLinks(_cache, categoryId);

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetArchivedLinks(Guid categoryId) =>
        ContentDataQueries.GetArchivedLinks(_cache, categoryId);

    /// <inheritdoc />
    public IReadOnlyList<SavedLink> GetAllArchivedLinks() =>
        ContentDataQueries.GetAllArchivedLinks(_cache);

    /// <inheritdoc />
    public async Task<SavedLink> AddLinkAsync(
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

        if (IsMissingResource(response.StatusCode))
        {
            throw new KeyNotFoundException($"Category '{categoryId}' was not found.");
        }

        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        var link = await response.Content.ReadFromJsonAsync<SavedLink>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created link.");

        await ReloadFromServerAsync(cancellationToken);
        return link;
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/links/{linkId}/archive", null, cancellationToken);
        if (IsMissingResource(response.StatusCode))
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/links/{linkId}/restore", null, cancellationToken);
        if (IsMissingResource(response.StatusCode))
        {
            return false;
        }

        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task ToggleLinkFavoriteAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/links/{linkId}/favorite", null, cancellationToken);
        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateLinkPreviewAsync(Guid linkId, LinkPreviewData preview, CancellationToken cancellationToken = default)
    {
        var response = await Http.PutAsJsonAsync($"/api/links/{linkId}/preview", preview, cancellationToken);
        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
        await ReloadFromServerAsync(cancellationToken);
    }

    private async Task ReloadFromServerAsync(CancellationToken cancellationToken)
    {
        var response = await Http.GetAsync("/api/bootstrap", cancellationToken);
        await EnsureAuthorizedAsync(response);
        EnsureSuccess(response);
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

    private static bool IsMissingResource(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden;

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConcurrencyConflictException();
        }

        response.EnsureSuccessStatusCode();
    }

    private void NotifyChanged() => DataChanged?.Invoke();
}
