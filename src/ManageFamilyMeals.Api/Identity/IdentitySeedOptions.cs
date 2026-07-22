namespace ManageFamilyMeals.Api.Identity;

public sealed class IdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";

    public const string DefaultDevPassword = "DevPassword1!";

    public string DefaultUserEmail { get; set; } = "dev@mfm.local";

    public string DefaultUserPassword { get; set; } = DefaultDevPassword;

    public string DefaultUserDisplayName { get; set; } = "Default Dev User";
}
