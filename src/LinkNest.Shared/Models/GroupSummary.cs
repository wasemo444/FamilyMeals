namespace LinkNest.Shared.Models;

/// <summary>
/// Lightweight projection of a family group returned by the groups API for selection and display.
/// </summary>
public sealed class GroupSummary
{
    /// <summary>Unique group identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>User-visible group name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Code used to invite new members to the group.</summary>
    public string InviteCode { get; set; } = string.Empty;

    /// <summary>User id of the member who originally created the group.</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>UTC timestamp when the group was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Calling user's role within this group.</summary>
    public GroupRole CurrentUserRole { get; set; }
}
