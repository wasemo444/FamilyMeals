namespace LinkNest.Shared.Auth;

/// <summary>
/// JWT token response returned by the mobile login endpoint.
/// </summary>
public sealed class AuthTokenResponse
{
    /// <summary>Bearer access token value.</summary>
    public required string AccessToken { get; init; }

    /// <summary>UTC expiration timestamp for the access token.</summary>
    public required DateTime ExpiresAtUtc { get; init; }

    /// <summary>Authenticated user profile.</summary>
    public required AuthUserInfo User { get; init; }
}
