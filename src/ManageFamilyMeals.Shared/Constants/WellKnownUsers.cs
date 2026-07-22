namespace ManageFamilyMeals.Shared.Constants;

public static class WellKnownUsers
{
    /// <summary>
    /// Fixed id for the seeded default user. E3 will backfill ownership columns to this user.
    /// </summary>
    public static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public const string DefaultUserEmail = "dev@mfm.local";
}
