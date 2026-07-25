using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Tests.Helpers;

internal static class TestOwnershipDefaults
{
    public static void ApplyUserOwnership(MealCategory category, Guid? userId = null)
    {
        category.OwnerType = OwnerType.User;
        category.OwnerUserId = userId ?? WellKnownUsers.DefaultUserId;
        category.OwnerGroupId = null;
    }

    public static void ApplyUserOwnership(MealLink link, Guid? userId = null)
    {
        link.OwnerType = OwnerType.User;
        link.OwnerUserId = userId ?? WellKnownUsers.DefaultUserId;
        link.OwnerGroupId = null;
    }
}
