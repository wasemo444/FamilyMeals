using LinkNest.Shared.Models;

namespace LinkNest.Api.Data.Entities;

/// <summary>
/// Email invite for a registered user to join a group.
/// </summary>
public sealed class GroupInviteEntity
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public GroupEntity Group { get; set; } = default!;

    public Guid InviteeUserId { get; set; }

    public Guid InvitedByUserId { get; set; }

    public GroupInviteStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }
}
