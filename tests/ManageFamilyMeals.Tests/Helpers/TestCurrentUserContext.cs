using ManageFamilyMeals.Shared.Constants;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Tests.Helpers;

internal sealed class TestCurrentUserContext : ICurrentUserContext
{
    public Guid? UserId { get; set; } = WellKnownUsers.DefaultUserId;

    public Guid GetRequiredUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
}
