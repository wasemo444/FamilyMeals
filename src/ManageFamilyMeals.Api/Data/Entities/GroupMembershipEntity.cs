using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Api.Data.Entities;

/// <summary>
/// Persistence model linking a user to a group with a role (for example, admin or member).
/// </summary>
public sealed class GroupMembershipEntity
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public GroupEntity Group { get; set; } = default!;

    public Guid UserId { get; set; }

    public GroupRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
