using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// Static ownership and home-page filter rules applied consistently on client and server.
/// </summary>
public static class OwnershipRules
{
    /// <summary>
    /// Determines whether the current user may mutate content with the given ownership fields.
    /// </summary>
    /// <param name="ownerType">Personal or group ownership.</param>
    /// <param name="ownerUserId">User id when <paramref name="ownerType"/> is <see cref="OwnerType.User"/>.</param>
    /// <param name="ownerGroupId">Group id when <paramref name="ownerType"/> is <see cref="OwnerType.Group"/>.</param>
    /// <param name="currentUserId">Authenticated user's id.</param>
    /// <param name="memberGroupIds">Groups the user belongs to.</param>
    /// <returns><see langword="true"/> when mutation is permitted.</returns>
    public static bool CanMutate(
        OwnerType ownerType,
        Guid? ownerUserId,
        Guid? ownerGroupId,
        Guid currentUserId,
        IReadOnlySet<Guid> memberGroupIds) =>
        ownerType switch
        {
            OwnerType.User => ownerUserId == currentUserId,
            OwnerType.Group => ownerGroupId is not null && memberGroupIds.Contains(ownerGroupId.Value),
            _ => false
        };

    /// <summary>
    /// Determines whether the current user may mutate the given category.
    /// </summary>
    /// <param name="category">Category whose ownership is evaluated.</param>
    /// <param name="currentUserId">Authenticated user's id.</param>
    /// <param name="memberGroupIds">Groups the user belongs to.</param>
    public static bool CanMutate(ContentCategory category, Guid currentUserId, IReadOnlySet<Guid> memberGroupIds) =>
        CanMutate(category.OwnerType, category.OwnerUserId, category.OwnerGroupId, currentUserId, memberGroupIds);

    /// <summary>
    /// Determines whether the current user may mutate the given link.
    /// </summary>
    /// <param name="link">Link whose ownership is evaluated.</param>
    /// <param name="currentUserId">Authenticated user's id.</param>
    /// <param name="memberGroupIds">Groups the user belongs to.</param>
    public static bool CanMutate(SavedLink link, Guid currentUserId, IReadOnlySet<Guid> memberGroupIds) =>
        CanMutate(link.OwnerType, link.OwnerUserId, link.OwnerGroupId, currentUserId, memberGroupIds);

    /// <summary>
    /// Validates that the user may create content under the requested ownership scope.
    /// </summary>
    /// <param name="owner">Target ownership for new content.</param>
    /// <param name="currentUserId">Authenticated user's id.</param>
    /// <param name="memberGroupIds">Groups the user belongs to.</param>
    /// <exception cref="UnauthorizedAccessException">The user is not a member of the requested group.</exception>
    /// <remarks>Personal ownership always passes; group ownership requires membership.</remarks>
    public static void ValidateCreateOwner(
        ContentOwner owner,
        Guid currentUserId,
        IReadOnlySet<Guid> memberGroupIds)
    {
        if (owner.OwnerType == OwnerType.User)
        {
            return;
        }

        if (owner.OwnerGroupId is null || !memberGroupIds.Contains(owner.OwnerGroupId.Value))
        {
            throw new UnauthorizedAccessException("You are not a member of the selected group.");
        }
    }

    /// <summary>
    /// Determines whether a category matches the home-page content filter.
    /// </summary>
    /// <param name="category">Category to test.</param>
    /// <param name="filter">Active filter selection.</param>
    public static bool MatchesFilter(ContentCategory category, HomeContentFilter filter) =>
        filter switch
        {
            HomeContentFilter.Mine => category.OwnerType == OwnerType.User,
            HomeContentFilter.Shared => category.OwnerType == OwnerType.Group,
            _ => true
        };
}
