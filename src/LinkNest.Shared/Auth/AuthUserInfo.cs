namespace LinkNest.Shared.Auth;

/// <summary>
/// Authenticated user profile returned by register, login, and current-user API endpoints.
/// </summary>
public sealed class AuthUserInfo
{
    /// <summary>Stable user identifier used for ownership and authorization.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized email address used for sign-in.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional display name shown in the UI header.</summary>
    public string? DisplayName { get; set; }
}
