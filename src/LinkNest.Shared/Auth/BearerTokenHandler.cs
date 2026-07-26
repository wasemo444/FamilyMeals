using System.Net.Http.Headers;
using LinkNest.Shared.Services;

namespace LinkNest.Shared.Auth;

/// <summary>
/// Attaches the stored JWT bearer token to outgoing API requests.
/// </summary>
public sealed class BearerTokenHandler(ISecureTokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
