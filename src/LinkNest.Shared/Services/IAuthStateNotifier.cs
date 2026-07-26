using LinkNest.Shared.Auth;

namespace LinkNest.Shared.Services;

/// <summary>
/// Notifies the Blazor authentication state provider when bearer-token sessions change.
/// </summary>
public interface IAuthStateNotifier
{
    /// <summary>Updates auth state after a successful JWT login.</summary>
    Task NotifySignedInAsync(AuthUserInfo user, CancellationToken cancellationToken = default);

    /// <summary>Clears auth state after logout.</summary>
    Task NotifySignedOutAsync(CancellationToken cancellationToken = default);
}
