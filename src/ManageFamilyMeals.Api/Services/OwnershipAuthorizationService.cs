using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Services;

public sealed class OwnershipAuthorizationService(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IOwnershipAuthorization
{
    public async Task<IReadOnlySet<Guid>> GetMemberGroupIdsAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();

        var groupIds = await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Select(membership => membership.GroupId)
            .ToListAsync(cancellationToken);

        return groupIds.ToHashSet();
    }

    public async Task ValidateCreateOwnerAsync(ContentOwner owner, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();
        var memberGroupIds = await GetMemberGroupIdsAsync(cancellationToken);
        OwnershipRules.ValidateCreateOwner(owner, userId, memberGroupIds);
    }

    public async Task EnsureCanMutateCategoryAsync(MealCategory category, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();
        var memberGroupIds = await GetMemberGroupIdsAsync(cancellationToken);

        if (!OwnershipRules.CanMutate(category, userId, memberGroupIds))
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this category.");
        }
    }

    public async Task EnsureCanMutateLinkAsync(MealLink link, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();
        var memberGroupIds = await GetMemberGroupIdsAsync(cancellationToken);

        if (!OwnershipRules.CanMutate(link, userId, memberGroupIds))
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this link.");
        }
    }
}
