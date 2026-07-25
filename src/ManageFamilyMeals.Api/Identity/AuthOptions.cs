namespace ManageFamilyMeals.Api.Identity;

/// <summary>
/// Configuration options for authentication behavior bound from the <c>Auth</c> configuration section.
/// </summary>
public sealed class AuthOptions
{
    /// <summary>Configuration section name (<c>Auth</c>).</summary>
    public const string SectionName = "Auth";

    public bool AllowRegistration { get; set; } = true;

    public bool RequireConfirmedEmail { get; set; } = true;

    public string WebBaseUrl { get; set; } = "http://localhost:5084/";
}
