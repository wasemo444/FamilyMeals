using Microsoft.AspNetCore.Components;
using ManageFamilyMeals.Web.Client.Services;

namespace ManageFamilyMeals.Web.Client;

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
