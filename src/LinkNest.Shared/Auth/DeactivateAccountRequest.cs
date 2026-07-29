namespace LinkNest.Shared.Auth;

/// <summary>
/// Request to soft-deactivate the authenticated user's account.
/// </summary>
public sealed class DeactivateAccountRequest
{
    public string Password { get; set; } = string.Empty;

    public string Confirmation { get; set; } = string.Empty;
}
