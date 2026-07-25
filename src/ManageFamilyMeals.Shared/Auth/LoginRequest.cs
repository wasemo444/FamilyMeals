namespace ManageFamilyMeals.Shared.Auth;

/// <summary>
/// Request body for the login API endpoint consumed by <see cref="Services.IAuthClient"/>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>Account email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>When <see langword="true"/>, extends the authentication cookie lifetime.</summary>
    public bool RememberMe { get; set; }
}
