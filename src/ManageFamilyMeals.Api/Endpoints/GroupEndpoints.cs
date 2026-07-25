using System.Security.Cryptography;
using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Api.Mapping;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Endpoints;

public static class GroupEndpoints
{
    private const string InviteCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/groups").RequireAuthorization();

        group.MapPost("/", CreateGroupAsync);
        group.MapGet("/", ListGroupsAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateGroupAsync(
        CreateGroupRequest request,
        AppDbContext dbContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required." });
        }

        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 200)
        {
            return Results.BadRequest(new { error = "Name must be 200 characters or fewer." });
        }

        var userId = currentUser.GetRequiredUserId();
        var now = DateTime.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var groupId = Guid.NewGuid();
            var entity = new GroupEntity
            {
                Id = groupId,
                Name = trimmedName,
                InviteCode = GenerateInviteCode(),
                CreatedByUserId = userId,
                CreatedAtUtc = now,
                Memberships =
                [
                    new GroupMembershipEntity
                    {
                        Id = Guid.NewGuid(),
                        GroupId = groupId,
                        UserId = userId,
                        Role = GroupRole.Admin,
                        JoinedAtUtc = now
                    }
                ]
            };

            dbContext.Groups.Add(entity);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Created($"/api/groups/{entity.Id}", entity.ToSummary(GroupRole.Admin));
            }
            catch (DbUpdateException) when (attempt < 4)
            {
                dbContext.Entry(entity).State = EntityState.Detached;
            }
        }

        return Results.Problem(
            detail: "Unable to create group due to invite code generation conflicts. Try again.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> ListGroupsAsync(
        AppDbContext dbContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        var groups = await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Select(membership => new
            {
                membership.Group,
                membership.Role
            })
            .OrderBy(item => item.Group.Name)
            .ToListAsync(cancellationToken);

        var summaries = groups
            .Select(item => item.Group.ToSummary(item.Role))
            .ToList();

        return Results.Ok(summaries);
    }

    private static string GenerateInviteCode()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = InviteCodeAlphabet[RandomNumberGenerator.GetInt32(InviteCodeAlphabet.Length)];
        }

        return new string(chars);
    }

    private sealed record CreateGroupRequest(string Name);
}
