using LinkNest.Shared.Services;
using Microsoft.Maui.Storage;

namespace LinkNest.Mobile.Services;

/// <summary>
/// Stores JWT access tokens in MAUI <see cref="Microsoft.Maui.Storage.SecureStorage"/>.
/// </summary>
public sealed class MauiSecureTokenStore : ISecureTokenStore
{
    private const string AccessTokenKey = "linknest.accessToken";
    private const string ExpiresAtKey = "linknest.tokenExpiresAtUtc";

    /// <inheritdoc />
    public async Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(ExpiresAtKey, expiresAtUtc.ToString("O"));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var expiresAtRaw = await SecureStorage.Default.GetAsync(ExpiresAtKey);
        if (string.IsNullOrWhiteSpace(expiresAtRaw)
            || !DateTime.TryParse(expiresAtRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAtUtc)
            || expiresAtUtc <= DateTime.UtcNow)
        {
            await ClearAsync(cancellationToken);
            return null;
        }

        return token;
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(ExpiresAtKey);
        return Task.CompletedTask;
    }
}
