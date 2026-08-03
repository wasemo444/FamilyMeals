namespace LinkNest.Shared.Auth;

/// <summary>
/// Request to confirm a user's email address using a token from email.
/// </summary>
public sealed class ConfirmEmailRequest
{
    public Guid UserId { get; set; }

    public string Code { get; set; } = string.Empty;
}
