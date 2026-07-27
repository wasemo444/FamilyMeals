namespace LinkNest.Shared.Services;

/// <summary>
/// Platform-specific secure storage for bearer access tokens (MAUI SecureStorage, etc.).
/// </summary>
public interface ISecureTokenStore
{
    /// <summary>Persists the access token and its UTC expiration.</summary>
    Task SaveAsync(string accessToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a non-expired access token, if one exists.</summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes any stored token.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
