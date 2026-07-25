using System.Security.Claims;
using ManageFamilyMeals.Api.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

namespace ManageFamilyMeals.Web.Auth;

public sealed class PersistingRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory), IHostEnvironmentAuthenticationStateProvider
{
    private Task<AuthenticationState>? _authenticationStateTask;

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    public new void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask) =>
        _authenticationStateTask = authenticationStateTask;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_authenticationStateTask is not null)
        {
            var authenticationState = await _authenticationStateTask;
            _authenticationStateTask = null;
            return authenticationState;
        }

        var httpContextUser = httpContextAccessor.HttpContext?.User;
        if (httpContextUser?.Identity?.IsAuthenticated == true)
        {
            return new AuthenticationState(httpContextUser);
        }

        try
        {
            return await base.GetAuthenticationStateAsync();
        }
        catch (InvalidOperationException)
        {
            var user = httpContextUser ?? new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(user);
        }
    }

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateScope();
        using var _ = scope;
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(authenticationState.User);
        return user is not null;
    }
}
