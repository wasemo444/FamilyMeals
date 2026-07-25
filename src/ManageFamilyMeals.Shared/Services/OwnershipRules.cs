using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public static class OwnershipRules
{
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

    public static bool CanMutate(MealCategory category, Guid currentUserId, IReadOnlySet<Guid> memberGroupIds) =>
        CanMutate(category.OwnerType, category.OwnerUserId, category.OwnerGroupId, currentUserId, memberGroupIds);

    public static bool CanMutate(MealLink link, Guid currentUserId, IReadOnlySet<Guid> memberGroupIds) =>
        CanMutate(link.OwnerType, link.OwnerUserId, link.OwnerGroupId, currentUserId, memberGroupIds);

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

    public static bool MatchesFilter(MealCategory category, HomeContentFilter filter) =>
        filter switch
        {
            HomeContentFilter.Mine => category.OwnerType == OwnerType.User,
            HomeContentFilter.Shared => category.OwnerType == OwnerType.Group,
            _ => true
        };
}
