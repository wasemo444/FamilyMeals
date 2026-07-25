namespace ManageFamilyMeals.Shared.Models;

/// <summary>
/// Filter applied on the home page to show personal categories, group-shared categories, or both.
/// </summary>
public enum HomeContentFilter
{
    /// <summary>Include personal and group-shared categories.</summary>
    All = 0,

    /// <summary>Include only categories owned by the current user.</summary>
    Mine = 1,

    /// <summary>Include only categories owned by a group the user belongs to.</summary>
    Shared = 2
}
