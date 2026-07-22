namespace ManageFamilyMeals.Api.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool AllowRegistration { get; set; } = true;
}
