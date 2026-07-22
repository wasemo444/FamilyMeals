namespace ManageFamilyMeals.Shared.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
