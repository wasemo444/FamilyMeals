using System.Text.Json;
using ManageFamilyMeals.Shared.Models;
using Microsoft.JSInterop;

namespace ManageFamilyMeals.Web.Client.Services;

public sealed class LocalStorageAppDataStore(IJSRuntime jsRuntime) : ManageFamilyMeals.Shared.Services.IAppDataStore
{
    private const string StorageKey = "manage-family-meals-data";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private IJSObjectReference? _module;

    public async Task<AppData?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        var json = await module.InvokeAsync<string?>("getItem", cancellationToken, StorageKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AppData>(json, JsonOptions);
    }

    public async Task SaveAsync(AppData data, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await module.InvokeVoidAsync("setItem", cancellationToken, StorageKey, json);
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module is null)
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/storage.js");
        }

        return _module;
    }
}
