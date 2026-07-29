namespace LinkNest.Shared.Auth;

/// <summary>
/// Request to update the authenticated user's profile fields.
/// </summary>
public sealed class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
}
