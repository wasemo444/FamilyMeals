namespace LinkNest.Shared.Services;

/// <summary>
/// Provides the authenticated user's id for ownership assignment and authorization checks.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Current user's id, or <see langword="null"/> when not authenticated.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// Returns the authenticated user's id, throwing when no user is signed in.
    /// </summary>
    /// <returns>Non-null user id.</returns>
    /// <exception cref="InvalidOperationException">No authenticated user is available.</exception>
    Guid GetRequiredUserId();
}
