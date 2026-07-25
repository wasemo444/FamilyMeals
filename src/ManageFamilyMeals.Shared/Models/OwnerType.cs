namespace ManageFamilyMeals.Shared.Models;

/// <summary>
/// Discriminator for content ownership scope on categories and links.
/// </summary>
public enum OwnerType
{
    /// <summary>Content owned by a single user and visible only to that user.</summary>
    User = 0,

    /// <summary>Content owned by a group and visible to all group members.</summary>
    Group = 1
}
