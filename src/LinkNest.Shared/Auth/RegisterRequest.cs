namespace LinkNest.Shared.Auth;

/// <summary>
/// Request body for the registration API endpoint consumed by <see cref="Services.IAuthClient"/>.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>Email address for the new account.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Chosen password meeting server-side complexity rules.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Confirmation of <see cref="Password"/>; must match for registration to succeed.</summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Optional display name stored on the user profile.</summary>
    public string? DisplayName { get; set; }
}
