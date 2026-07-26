using LinkNest.Shared.Services;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// No-op secure token store for cookie-based web clients.
/// </summary>
public sealed class WebSecureTokenStore : ISecureTokenStore
{
    public Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
