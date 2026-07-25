namespace ManageFamilyMeals.Api.Identity;

/// <summary>
/// Configuration options for the default seeded user bound from the <c>IdentitySeed</c> section.
/// </summary>
public sealed class IdentitySeedOptions
{
    /// <summary>Configuration section name (<c>IdentitySeed</c>).</summary>
    public const string SectionName = "IdentitySeed";

    /// <summary>Built-in development password that must not be used in production.</summary>
    public const string DefaultDevPassword = "DevPassword1!";

    public string DefaultUserEmail { get; set; } = "dev@mfm.local";

    public string DefaultUserPassword { get; set; } = DefaultDevPassword;

    public string DefaultUserDisplayName { get; set; } = "Default Dev User";
}
