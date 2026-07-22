namespace ManageFamilyMeals.Shared.Auth;

public sealed class AuthUserInfo
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
