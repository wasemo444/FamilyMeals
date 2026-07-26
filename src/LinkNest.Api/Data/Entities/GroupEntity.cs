using LinkNest.Shared.Models;

namespace LinkNest.Api.Data.Entities;

/// <summary>
/// Persistence model for a collaboration group with a unique invite code and creator reference.
/// </summary>
public sealed class GroupEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string InviteCode { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<GroupMembershipEntity> Memberships { get; set; } = [];

    public ICollection<GroupInviteEntity> Invites { get; set; } = [];
}
