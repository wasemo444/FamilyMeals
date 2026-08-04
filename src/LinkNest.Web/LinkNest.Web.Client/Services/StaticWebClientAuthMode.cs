using LinkNest.Shared.Services;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// Bearer-token auth mode for the standalone Blazor WASM client (Cloudflare Pages).
/// </summary>
public sealed class StaticWebClientAuthMode : IClientAuthMode
{
    /// <inheritdoc />
    public bool UsesBearerToken => true;
}
