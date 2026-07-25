using ManageFamilyMeals.Api.Data.Entities;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;
using System.Security.Claims;

namespace ManageFamilyMeals.Api.Services;

/// <summary>
/// Resolves the authenticated user identifier from the current HTTP context claims.
/// </summary>
public sealed class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    /// <summary>
    /// Gets the current user's id from the name-identifier claim, or <see langword="null"/> when unauthenticated.
    /// </summary>
    public Guid? UserId
    {
        get
        {
            var userIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Gets the current user's id, requiring an authenticated principal.
    /// </summary>
    /// <returns>The authenticated user's unique identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when no valid user id claim is present.</exception>
    public Guid GetRequiredUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
}
