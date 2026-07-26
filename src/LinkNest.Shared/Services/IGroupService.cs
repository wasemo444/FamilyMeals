using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// Client for group and membership operations via the groups API.
/// </summary>
public interface IGroupService
{
    /// <summary>Returns all groups the current user belongs to.</summary>
    Task<IReadOnlyList<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new group with the current user as admin.</summary>
    Task<GroupSummary> CreateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Returns members of a group the caller belongs to.</summary>
    Task<IReadOnlyList<GroupMemberSummary>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Invites a registered user by email (admin only).</summary>
    Task InviteMemberAsync(Guid groupId, string email, CancellationToken cancellationToken = default);

    /// <summary>Returns pending invites for the current user.</summary>
    Task<IReadOnlyList<GroupInviteSummary>> GetPendingInvitesAsync(CancellationToken cancellationToken = default);

    /// <summary>Accepts a pending group invite.</summary>
    Task AcceptInviteAsync(Guid inviteId, CancellationToken cancellationToken = default);

    /// <summary>Declines a pending group invite.</summary>
    Task DeclineInviteAsync(Guid inviteId, CancellationToken cancellationToken = default);

    /// <summary>Removes a member from the group (admin only).</summary>
    Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Leaves the group as the current user.</summary>
    Task LeaveGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
