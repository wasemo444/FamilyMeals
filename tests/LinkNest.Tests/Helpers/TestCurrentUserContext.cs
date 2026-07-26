using LinkNest.Shared.Constants;
using LinkNest.Shared.Services;

namespace LinkNest.Tests.Helpers;

internal sealed class TestCurrentUserContext : ICurrentUserContext
{
    public Guid? UserId { get; set; } = WellKnownUsers.DefaultUserId;

    public Guid GetRequiredUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
}
