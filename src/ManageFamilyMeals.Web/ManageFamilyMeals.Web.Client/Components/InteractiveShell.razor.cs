using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ManageFamilyMeals.Web.Client.Components;

public partial class InteractiveShell : IDisposable
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    protected bool IsReady { get; private set; }

    private bool _initialized;
    private int _cultureVersion;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized)
        {
            return;
        }

        await DataService.InitializeAsync();
        await CultureService.InitializeAsync(DataService.GetSettings());
        IsReady = true;
        _initialized = true;
        StateHasChanged();
    }

    protected override void OnCultureChanged()
    {
        _cultureVersion++;
        base.OnCultureChanged();
    }

    public new void Dispose()
    {
        base.Dispose();
    }
}
