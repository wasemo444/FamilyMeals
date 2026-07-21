using System.Globalization;
using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.JSInterop;

namespace ManageFamilyMeals.Web.Client.Services;

public sealed class CultureService(
    IJSRuntime jsRuntime,
    CultureState cultureState,
    IHttpClientFactory httpClientFactory,
    IMealDataService mealDataService)
{
    private IJSObjectReference? _module;

    private HttpClient Http => httpClientFactory.CreateClient("MealDataApi");

    public event Action? CultureChanged
    {
        add => cultureState.Changed += value;
        remove => cultureState.Changed -= value;
    }

    public CultureInfo CurrentCulture => cultureState.CurrentCulture;

    public bool IsRightToLeft => cultureState.IsRightToLeft;

    public async Task InitializeAsync(AppSettings settings)
    {
        var module = await GetModuleAsync();
        var browserCulture = await module.InvokeAsync<string?>("getBrowserLanguage");

        var cultureCode = settings.CultureCode
            ?? NormalizeCulture(browserCulture)
            ?? "en";

        await ApplyCultureAsync(cultureCode, persist: false);
    }

    public async Task SetCultureAsync(string cultureCode)
    {
        await ApplyCultureAsync(cultureCode, persist: true);
    }

    private async Task ApplyCultureAsync(string cultureCode, bool persist)
    {
        cultureState.SetCulture(cultureCode);

        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(
            "setDocumentCulture",
            cultureState.CultureCode,
            cultureState.IsRightToLeft);

        if (persist)
        {
            var response = await Http.PutAsJsonAsync("/api/settings", new AppSettings
            {
                CultureCode = cultureState.CultureCode
            });
            response.EnsureSuccessStatusCode();

            var settings = await response.Content.ReadFromJsonAsync<AppSettings>()
                ?? new AppSettings { CultureCode = cultureState.CultureCode };
            mealDataService.ApplySettings(settings);
        }
    }

    private static string? NormalizeCulture(string? cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return null;
        }

        if (cultureCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return "ar";
        }

        if (cultureCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return null;
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
