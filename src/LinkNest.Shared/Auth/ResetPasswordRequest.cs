namespace LinkNest.Shared.Auth;

/// <summary>
/// Request to set a new password using a reset token from email.
/// </summary>
public sealed class ResetPasswordRequest
{
    public Guid UserId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;
}
