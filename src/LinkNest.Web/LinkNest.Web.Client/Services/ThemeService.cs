using Microsoft.JSInterop;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// Persists and applies light/dark theme via <c>data-theme</c> on the document root.
/// </summary>
public sealed class ThemeService(IJSRuntime jsRuntime)
{
    public string Theme { get; private set; } = "dark";

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        Theme = await jsRuntime.InvokeAsync<string>("linknestTheme.get");
        await jsRuntime.InvokeVoidAsync("linknestTheme.applyStored");
        Changed?.Invoke();
    }

    public async Task ToggleAsync()
    {
        Theme = Theme == "dark" ? "light" : "dark";
        await jsRuntime.InvokeVoidAsync("linknestTheme.set", Theme);
        Changed?.Invoke();
    }

    public bool IsDark => Theme == "dark";
}
