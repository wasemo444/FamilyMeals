namespace ManageFamilyMeals.Shared.Constants;

/// <summary>
/// Identifiers for seeded development users used during local setup and ownership backfill.
/// </summary>
public static class WellKnownUsers
{
    /// <summary>
    /// Fixed id for the seeded default user. Ownership backfill assigns legacy rows to this user.
    /// </summary>
    public static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Email address of the seeded default development account.</summary>
    public const string DefaultUserEmail = "dev@mfm.local";
}
