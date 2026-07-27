using System.Net;
using LinkNest.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Shared.Auth;

/// <summary>
/// Clears stored bearer tokens when the API returns 401 Unauthorized.
/// </summary>
public sealed class UnauthorizedSessionHandler(IServiceScopeFactory scopeFactory) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        using var scope = scopeFactory.CreateScope();
        var tokenStore = scope.ServiceProvider.GetRequiredService<ISecureTokenStore>();
        var authStateNotifier = scope.ServiceProvider.GetRequiredService<IAuthStateNotifier>();
        await tokenStore.ClearAsync(cancellationToken);
        await authStateNotifier.NotifySignedOutAsync(cancellationToken);
        return response;
    }
}
