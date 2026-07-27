using LinkNest.Shared.Services;

namespace LinkNest.Mobile.Services;

/// <summary>
/// Bearer-token auth mode for the MAUI client.
/// </summary>
public sealed class MobileClientAuthMode : IClientAuthMode
{
    /// <inheritdoc />
    public bool UsesBearerToken => true;
}
