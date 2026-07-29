using System.Security.Claims;
using LinkNest.Api.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

namespace LinkNest.Web.Auth;

/// <summary>
/// Server-side authentication state provider that revalidates users periodically and accepts
/// authentication state handoff from the WebAssembly client.
/// </summary>
/// <remarks>
/// Implements <see cref="IHostEnvironmentAuthenticationStateProvider"/> so serialized auth state
/// from interactive WebAssembly components can be consumed on the server during prerendering.
/// Falls back to <see cref="IHttpContextAccessor"/> when no handoff task is pending.
/// Revalidates every 30 minutes by confirming the user still exists in Identity.
/// </remarks>
public sealed class PersistingRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory), IHostEnvironmentAuthenticationStateProvider
{
    private Task<AuthenticationState>? _authenticationStateTask;

    /// <summary>Interval between authentication revalidation checks.</summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <summary>Accepts authentication state serialized from the WebAssembly client.</summary>
    /// <param name="authenticationStateTask">Task producing the handoff authentication state.</param>
    public new void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask) =>
        _authenticationStateTask = authenticationStateTask;

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateScope();
        using var _ = scope;
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(authenticationState.User);
        return user is not null && user.IsActive;
    }
}
