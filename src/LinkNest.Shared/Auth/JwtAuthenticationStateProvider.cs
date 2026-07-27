using System.Security.Claims;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace LinkNest.Shared.Auth;

/// <summary>
/// Blazor authentication state backed by a JWT stored in secure platform storage.
/// </summary>
public sealed class JwtAuthenticationStateProvider(
    ISecureTokenStore tokenStore,
    IAuthClient authClient) : AuthenticationStateProvider, IAuthStateNotifier
{
    private AuthUserInfo? _currentUser;

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser is not null)
        {
            return CreateAuthenticationState(_currentUser);
        }

        var token = await tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return CreateAnonymousState();
        }

        try
        {
            var user = await authClient.GetCurrentUserAsync();
            if (user is null)
            {
                await tokenStore.ClearAsync();
                return CreateAnonymousState();
            }

            _currentUser = user;
            return CreateAuthenticationState(user);
        }
        catch (UnauthorizedAccessException)
        {
            await tokenStore.ClearAsync();
            return CreateAnonymousState();
        }
        catch (HttpRequestException)
        {
            // API unreachable — keep stored token so a later retry can restore the session.
            return CreateAnonymousState();
        }
        catch (TaskCanceledException)
        {
            return CreateAnonymousState();
        }
    }

    /// <inheritdoc />
    public Task NotifySignedInAsync(AuthUserInfo user, CancellationToken cancellationToken = default)
    {
        _currentUser = user;
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(user)));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotifySignedOutAsync(CancellationToken cancellationToken = default)
    {
        _currentUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAnonymousState()));
        return Task.CompletedTask;
    }

    private static AuthenticationState CreateAuthenticationState(AuthUserInfo user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new("DisplayName", user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static AuthenticationState CreateAnonymousState() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
