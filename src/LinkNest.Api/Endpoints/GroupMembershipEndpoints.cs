using LinkNest.Api.Data;
using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using LinkNest.Api.Services;
using LinkNest.Shared.Models;
using LinkNest.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Endpoints;

/// <summary>
/// Invite, accept/decline, member list, leave, and admin remove endpoints for group membership (E5).
/// </summary>
public static class GroupMembershipEndpoints
{
    /// <summary>Maps group membership routes under <c>/api/groups</c>.</summary>
    public static IEndpointRouteBuilder MapGroupMembershipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/groups").RequireAuthorization();

        group.MapGet("/{groupId:guid}/members", ListMembersAsync);
        group.MapPost("/{groupId:guid}/invites", CreateInviteAsync);
        group.MapGet("/invites/pending", ListPendingInvitesAsync);
        group.MapPost("/invites/{inviteId:guid}/accept", AcceptInviteAsync);
        group.MapPost("/invites/{inviteId:guid}/decline", DeclineInviteAsync);
        group.MapDelete("/{groupId:guid}/members/{userId:guid}", RemoveMemberAsync);
        group.MapPost("/{groupId:guid}/leave", LeaveGroupAsync);

        return endpoints;
    }

    private static async Task<IResult> ListMembersAsync(
        Guid groupId,
        AppDbContext dbContext,
        GroupMembershipService membershipService,
        ICurrentUserContext currentUser,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        if (await membershipService.GetMembershipAsync(groupId, userId, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var members = await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.JoinedAtUtc)
            .ToListAsync(cancellationToken);

        var userIds = members.Select(m => m.UserId).ToList();
        var users = await userManager.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var summaries = members.Select(m =>
        {
            users.TryGetValue(m.UserId, out var user);
            return new GroupMemberSummary
            {
                UserId = m.UserId,
                DisplayName = user?.DisplayName ?? user?.Email ?? m.UserId.ToString(),
                Email = user?.Email ?? string.Empty,
                Role = m.Role,
                JoinedAtUtc = m.JoinedAtUtc
            };
        }).ToList();

        return Results.Ok(summaries);
    }

    private static async Task<IResult> CreateInviteAsync(
        Guid groupId,
        CreateInviteRequest request,
        AppDbContext dbContext,
        GroupMembershipService membershipService,
        UserManager<ApplicationUser> userManager,
        IOptions<AuthOptions> authOptions,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetRequiredUserId();
        if (!await membershipService.IsAdminAsync(groupId, adminId, cancellationToken))
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("email_required", "Email is required.");
        }

        var normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
        var invitee = await userManager.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (invitee is null)
        {
            return BadRequest("invitee_not_found", "No registered account matches that email.");
        }

        if (authOptions.Value.RequireConfirmedEmail && !invitee.EmailConfirmed)
        {
            return BadRequest("invitee_email_unconfirmed", "That user has not confirmed their email address.");
        }

        if (await membershipService.IsUserInAnyGroupAsync(invitee.Id, cancellationToken))
        {
            return BadRequest("invitee_in_group", "That user is already a member of a group.");
        }

        if (await membershipService.GetMembershipAsync(groupId, invitee.Id, cancellationToken) is not null)
        {
            return BadRequest("invitee_already_member", "That user is already a member of this group.");
        }

        if (await membershipService.IsGroupFullAsync(groupId, cancellationToken))
        {
            return BadRequest("group_full", $"This group has reached the maximum of {GroupPolicy.MaxMembers} members.");
        }

        var hasPendingInvite = await dbContext.GroupInvites.AnyAsync(
            i => i.GroupId == groupId
                 && i.InviteeUserId == invitee.Id
                 && i.Status == GroupInviteStatus.Pending,
            cancellationToken);

        if (hasPendingInvite)
        {
            return BadRequest("invite_already_pending", "An invite is already pending for that user.");
        }

        var invite = new GroupInviteEntity
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            InviteeUserId = invitee.Id,
            InvitedByUserId = adminId,
            Status = GroupInviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.GroupInvites.Add(invite);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/groups/invites/{invite.Id}", new { invite.Id });
    }

    private static async Task<IResult> ListPendingInvitesAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        var invites = await dbContext.GroupInvites
            .AsNoTracking()
            .Include(i => i.Group)
            .Where(i => i.InviteeUserId == userId && i.Status == GroupInviteStatus.Pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var inviterIds = invites.Select(i => i.InvitedByUserId).Distinct().ToList();
        var inviters = await userManager.Users
            .AsNoTracking()
            .Where(u => inviterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var summaries = invites.Select(i =>
        {
            inviters.TryGetValue(i.InvitedByUserId, out var inviter);
            return new GroupInviteSummary
            {
                Id = i.Id,
                GroupId = i.GroupId,
                GroupName = i.Group.Name,
                InvitedByDisplayName = inviter?.DisplayName ?? inviter?.Email ?? string.Empty,
                CreatedAtUtc = i.CreatedAtUtc
            };
        }).ToList();

        return Results.Ok(summaries);
    }

    private static async Task<IResult> AcceptInviteAsync(
        Guid inviteId,
        AppDbContext dbContext,
        GroupMembershipService membershipService,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        var invite = await dbContext.GroupInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);

        if (invite is null || invite.InviteeUserId != userId || invite.Status != GroupInviteStatus.Pending)
        {
            return Results.NotFound();
        }

        if (await membershipService.IsUserInAnyGroupAsync(userId, cancellationToken))
        {
            return BadRequest("invitee_in_group", "You are already a member of a group.");
        }

        if (await membershipService.IsGroupFullAsync(invite.GroupId, cancellationToken))
        {
            return BadRequest("group_full", $"This group has reached the maximum of {GroupPolicy.MaxMembers} members.");
        }

        var now = DateTime.UtcNow;
        invite.Status = GroupInviteStatus.Accepted;
        invite.RespondedAtUtc = now;

        dbContext.GroupMemberships.Add(new GroupMembershipEntity
        {
            Id = Guid.NewGuid(),
            GroupId = invite.GroupId,
            UserId = userId,
            Role = GroupRole.Member,
            JoinedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> DeclineInviteAsync(
        Guid inviteId,
        AppDbContext dbContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        var invite = await dbContext.GroupInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);

        if (invite is null || invite.InviteeUserId != userId || invite.Status != GroupInviteStatus.Pending)
        {
            return Results.NotFound();
        }

        invite.Status = GroupInviteStatus.Declined;
        invite.RespondedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid groupId,
        Guid userId,
        AppDbContext dbContext,
        GroupMembershipService membershipService,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetRequiredUserId();
        if (!await membershipService.IsAdminAsync(groupId, adminId, cancellationToken))
        {
            return Results.NotFound();
        }

        if (userId == adminId)
        {
            return BadRequest("cannot_remove_self", "Admins cannot remove themselves. Use leave instead.");
        }

        var membership = await dbContext.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

        if (membership is null)
        {
            return Results.NotFound();
        }

        dbContext.GroupMemberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> LeaveGroupAsync(
        Guid groupId,
        AppDbContext dbContext,
        GroupMembershipService membershipService,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        var membership = await dbContext.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

        if (membership is null)
        {
            return Results.NotFound();
        }

        dbContext.GroupMemberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    private static IResult BadRequest(string code, string error) =>
        Results.BadRequest(new { code, error });

    private sealed record CreateInviteRequest(string Email);
}
