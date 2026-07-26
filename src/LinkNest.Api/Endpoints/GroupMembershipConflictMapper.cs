using Microsoft.EntityFrameworkCore;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Maps database unique-constraint violations to E5 membership error codes.
/// </summary>
internal static class GroupMembershipConflictMapper
{
    internal const string PendingInviteUniqueIndexName = "IX_group_invites_GroupId_InviteeUserId_pending_unique";

    public static bool IsDuplicatePendingInvite(DbUpdateException exception) =>
        MatchesConstraint(exception, PendingInviteUniqueIndexName, "group_invites");

    public static bool IsDuplicateGroupMembership(DbUpdateException exception) =>
        MatchesConstraint(exception, "group_memberships", "GroupId", "UserId");

    private static bool MatchesConstraint(DbUpdateException exception, params string[] hints)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return hints.All(hint => message.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }
}
