using LinkNest.Shared.Constants;
using LinkNest.Shared.Models;

namespace LinkNest.Tests.Helpers;

internal static class TestOwnershipDefaults
{
    public static void ApplyUserOwnership(ContentCategory category, Guid? userId = null)
    {
        category.OwnerType = OwnerType.User;
        category.OwnerUserId = userId ?? WellKnownUsers.DefaultUserId;
        category.OwnerGroupId = null;
    }

    public static void ApplyUserOwnership(SavedLink link, Guid? userId = null)
    {
        link.OwnerType = OwnerType.User;
        link.OwnerUserId = userId ?? WellKnownUsers.DefaultUserId;
        link.OwnerGroupId = null;
    }
}
