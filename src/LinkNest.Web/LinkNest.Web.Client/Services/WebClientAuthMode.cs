using LinkNest.Shared.Services;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// Cookie-based auth mode used by the Blazor web client.
/// </summary>
public sealed class WebClientAuthMode : IClientAuthMode
{
    /// <inheritdoc />
    public bool UsesBearerToken => false;
}
