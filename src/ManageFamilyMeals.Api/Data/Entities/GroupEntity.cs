using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Api.Data.Entities;

public sealed class GroupEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string InviteCode { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<GroupMembershipEntity> Memberships { get; set; } = [];
}
