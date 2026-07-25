namespace ManageFamilyMeals.Api.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool AllowRegistration { get; set; } = true;

    public bool RequireConfirmedEmail { get; set; } = true;

    public string WebBaseUrl { get; set; } = "http://localhost:5084/";
}
