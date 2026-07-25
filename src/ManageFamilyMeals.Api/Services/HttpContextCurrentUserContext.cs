using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using System.Security.Claims;

namespace ManageFamilyMeals.Api.Services;

public sealed class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            var userIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }

    public Guid GetRequiredUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
}
