namespace ManageFamilyMeals.Shared.Models;

/// <summary>
/// Role assigned to a user within a family group, determining administrative privileges.
/// </summary>
public enum GroupRole
{
    /// <summary>Can manage group membership and group-owned content.</summary>
    Admin = 0,

    /// <summary>Can view and mutate group-owned content but cannot administer the group.</summary>
    Member = 1
}
