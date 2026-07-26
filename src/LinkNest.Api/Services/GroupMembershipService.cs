using LinkNest.Api.Data;
using LinkNest.Api.Data.Entities;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.Services;

/// <summary>
/// Enforces group membership rules: member cap and admin/member authorization.
/// </summary>
public sealed class GroupMembershipService(AppDbContext dbContext)
{
    /// <summary>Returns whether the user belongs to any group.</summary>
    public Task<bool> IsUserInAnyGroupAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.GroupMemberships.AnyAsync(m => m.UserId == userId, cancellationToken);

    /// <summary>Returns the current member count for a group.</summary>
    public Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        dbContext.GroupMemberships.CountAsync(m => m.GroupId == groupId, cancellationToken);

    /// <summary>Returns whether the group has reached the member cap.</summary>
    public async Task<bool> IsGroupFullAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var count = await GetMemberCountAsync(groupId, cancellationToken);
        return count >= GroupPolicy.MaxMembers;
    }

    /// <summary>Returns the caller's membership in the group, or null if not a member.</summary>
    public Task<GroupMembershipEntity?> GetMembershipAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.GroupMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

    /// <summary>Returns whether the user is an admin of the group.</summary>
    public async Task<bool> IsAdminAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipAsync(groupId, userId, cancellationToken);
        return membership?.Role == GroupRole.Admin;
    }
}
