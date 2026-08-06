using System.Security.Claims;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;
using LinkNest.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace LinkNest.Web.Client.Components;

/// <summary>
/// Authenticated application shell that initializes meal data and culture for child content.
/// </summary>
/// <remarks>
/// Wraps interactive pages in WebAssembly render mode. Defers culture and data initialization
/// until <see cref="RendererInfo.IsInteractive"/> is true because prerendering cannot call
/// JavaScript or authenticated API endpoints. Redirects to login on 401 responses from the API.
/// </remarks>
public partial class InteractiveShell : IDisposable
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IContentDataService DataService { get; set; } = default!;

    [Inject]
    private IAuthClient AuthClient { get; set; } = default!;

    [Inject]
    private IClientAuthMode ClientAuthMode { get; set; } = default!;

    [Inject]
    private ISecureTokenStore SecureTokenStore { get; set; } = default!;

    [Inject]
    private IAuthStateNotifier AuthStateNotifier { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    protected bool IsReady { get; private set; }

    protected bool UsesBearerToken => ClientAuthMode.UsesBearerToken;

    protected AuthUserInfo? _currentUser;

    private bool _initialized;
    private bool _cultureInitialized;
    private int _cultureVersion;

    private static string GetDisplayName(ClaimsPrincipal user) =>
        user.FindFirst("DisplayName")?.Value
        ?? user.FindFirst(ClaimTypes.Name)?.Value
        ?? user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.Identity?.Name
        ?? string.Empty;

    protected override async Task OnInitializedAsync()
    {
        if (DataService is ApiContentDataService apiDataService)
        {
            apiDataService.Unauthorized += RedirectToLoginAsync;
        }

        if (_initialized)
        {
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                _currentUser = UsesBearerToken
                    ? CreateUserFromClaims(authState.User)
                    : await AuthClient.GetCurrentUserAsync() ?? CreateUserFromClaims(authState.User);
                await DataService.InitializeAsync();
            }
            catch (UnauthorizedAccessException)
            {
                if (RendererInfo.IsInteractive)
                {
                    await RedirectToLoginAsync();
                    return;
                }
            }
        }
        else
        {
            IsReady = true;
            _initialized = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            await ThemeService.InitializeAsync();
        }

        if (_cultureInitialized || _initialized)
        {
            return;
        }

        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        try
        {
            await CultureService.InitializeAsync(DataService.GetSettings());
        }
        catch (UnauthorizedAccessException)
        {
            await RedirectToLoginAsync();
            return;
        }

        _cultureInitialized = true;
        IsReady = true;
        _initialized = true;
        StateHasChanged();
    }

    private static AuthUserInfo CreateUserFromClaims(ClaimsPrincipal user) => new()
    {
        Email = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.Identity?.Name
            ?? string.Empty,
        DisplayName = user.FindFirst("DisplayName")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity?.Name
            ?? string.Empty
    };

    private async Task RedirectToLoginAsync()
    {
        if (UsesBearerToken)
        {
            await SecureTokenStore.ClearAsync();
            await AuthStateNotifier.NotifySignedOutAsync();
        }

        var returnUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            returnUrl = "/";
        }

        NavigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: !UsesBearerToken);
    }

    protected async Task LogoutAsync()
    {
        if (!UsesBearerToken)
        {
            try
            {
                await AuthClient.LogoutAsync();
            }
            catch (HttpRequestException)
            {
                // Cookie session may already be cleared; still clear local state.
            }
        }

        await SecureTokenStore.ClearAsync();
        await AuthStateNotifier.NotifySignedOutAsync();
        NavigationManager.NavigateTo("/login");
    }

    protected override void OnCultureChanged()
    {
        _cultureVersion++;
        base.OnCultureChanged();
    }

    protected string NavLinkClass(string href)
    {
        var relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var normalized = string.IsNullOrEmpty(relative) ? "/" : "/" + relative.Split('?', '#')[0];

        if (href == "/")
        {
            return normalized == "/" ? "ln-nav-link--active" : string.Empty;
        }

        return normalized.StartsWith(href, StringComparison.OrdinalIgnoreCase)
            ? "ln-nav-link--active"
            : string.Empty;
    }

    public new void Dispose()
    {
        if (DataService is ApiContentDataService apiDataService)
        {
            apiDataService.Unauthorized -= RedirectToLoginAsync;
        }

        base.Dispose();
    }
}
