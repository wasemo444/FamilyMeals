using LinkNest.Shared.Auth;
using LinkNest.Shared.Services;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// No-op auth state notifier for cookie-based web clients.
/// </summary>
public sealed class WebAuthStateNotifier : IAuthStateNotifier
{
    public Task NotifySignedInAsync(AuthUserInfo user, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifySignedOutAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
