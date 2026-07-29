namespace LinkNest.Shared.Auth;

/// <summary>
/// Request to resend the email confirmation link for an unconfirmed account.
/// </summary>
public sealed class ResendConfirmationRequest
{
    public string Email { get; set; } = string.Empty;
}
