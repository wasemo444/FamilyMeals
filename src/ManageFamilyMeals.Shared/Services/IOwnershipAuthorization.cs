using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

/// <summary>
/// Validates that the current user may create or mutate content based on personal or group ownership rules.
/// </summary>
public interface IOwnershipAuthorization
{
    /// <summary>
    /// Returns the set of group ids the current user belongs to, used for authorization checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlySet<Guid>> GetMemberGroupIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the current user may create content under the requested ownership scope.
    /// </summary>
    /// <param name="owner">Target ownership for a new category or link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">The user is not a member of the requested group.</exception>
    Task ValidateCreateOwnerAsync(ContentOwner owner, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the current user may modify or archive the given category.
    /// </summary>
    /// <param name="category">Category to authorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">The user does not own or belong to the category's group.</exception>
    Task EnsureCanMutateCategoryAsync(MealCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the current user may modify or archive the given link.
    /// </summary>
    /// <param name="link">Link to authorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">The user does not own or belong to the link's group.</exception>
    Task EnsureCanMutateLinkAsync(MealLink link, CancellationToken cancellationToken = default);
}
