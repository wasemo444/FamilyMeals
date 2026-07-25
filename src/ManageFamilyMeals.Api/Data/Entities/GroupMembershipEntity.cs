using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Api.Data.Entities;

public sealed class GroupMembershipEntity
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public GroupEntity Group { get; set; } = default!;

    public Guid UserId { get; set; }

    public GroupRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
