namespace LinkNest.Shared.Auth;

/// <summary>
/// Request to initiate a password-reset email for an account.
/// </summary>
public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
