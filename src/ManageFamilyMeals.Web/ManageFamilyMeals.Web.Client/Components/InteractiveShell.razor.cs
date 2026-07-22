using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ManageFamilyMeals.Web.Client.Components;

public partial class InteractiveShell : IDisposable
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IMealDataService DataService { get; set; } = default!;

    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected bool IsReady { get; private set; }

    protected AuthUserInfo? _currentUser;

    private bool _initialized;
    private int _cultureVersion;

    protected override void OnInitialized()
    {
        if (DataService is ApiMealDataService apiDataService)
        {
            apiDataService.Unauthorized += RedirectToLoginAsync;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized)
        {
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                _currentUser = await AuthClient.GetCurrentUserAsync();
                await DataService.InitializeAsync();
                await CultureService.InitializeAsync(DataService.GetSettings());
            }
            catch (UnauthorizedAccessException)
            {
                await RedirectToLoginAsync();
                return;
            }
        }

        IsReady = true;
        _initialized = true;
        StateHasChanged();
    }

    private Task RedirectToLoginAsync()
    {
        var returnUrl = Uri.EscapeDataString(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
        NavigationManager.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
        return Task.CompletedTask;
    }

    private async Task LogoutAsync()
    {
        await AuthClient.LogoutAsync();
        NavigationManager.NavigateTo("/login", forceLoad: true);
    }

    protected override void OnCultureChanged()
    {
        _cultureVersion++;
        base.OnCultureChanged();
    }

    public new void Dispose()
    {
        if (DataService is ApiMealDataService apiDataService)
        {
            apiDataService.Unauthorized -= RedirectToLoginAsync;
        }

        base.Dispose();
    }
}
