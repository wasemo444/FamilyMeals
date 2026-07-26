using Microsoft.AspNetCore.Components;
using LinkNest.Web.Client.Services;

namespace LinkNest.Web.Client;

/// <summary>
/// Base class for interactive components that react to culture and localization changes.
/// </summary>
/// <remarks>
/// Subscribes to <see cref="CultureService"/> and <see cref="ILocalizedText"/> change events
/// and triggers a re-render. Used by pages and components rendered in interactive WebAssembly mode.
/// </remarks>
public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected ILocalizedText L { get; set; } = default!;

    [Inject]
    protected CultureService CultureService { get; set; } = default!;

    protected override void OnInitialized()
    {
        CultureService.CultureChanged += OnCultureChanged;
        L.Changed += OnCultureChanged;
    }

    protected virtual void OnCultureChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        CultureService.CultureChanged -= OnCultureChanged;
        L.Changed -= OnCultureChanged;
    }
}
