using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ManageFamilyMeals.Api.Services;

/// <summary>
/// Enforces ownership rules for meal categories and links based on the current user and group memberships.
/// </summary>
/// <remarks>
/// Callers typically catch <see cref="UnauthorizedAccessException"/> and map to HTTP <c>404 Not Found</c> at the endpoint layer.
/// </remarks>
public sealed class OwnershipAuthorizationService(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IOwnershipAuthorization
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the user cannot create content for the specified owner.</exception>
    public async Task ValidateCreateOwnerAsync(ContentOwner owner, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();
        var memberGroupIds = await GetMemberGroupIdsAsync(cancellationToken);
        OwnershipRules.ValidateCreateOwner(owner, userId, memberGroupIds);
    }

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the user cannot modify the category.</exception>
    public async Task EnsureCanMutateCategoryAsync(MealCategory category, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetRequiredUserId();
        var memberGroupIds = await GetMemberGroupIdsAsync(cancellationToken);

        if (!OwnershipRules.CanMutate(category, userId, memberGroupIds))
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this category.");
        }
    }

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the user cannot modify the link.</exception>
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
