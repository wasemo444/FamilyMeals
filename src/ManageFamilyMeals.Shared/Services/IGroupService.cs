using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

/// <summary>
/// Client for listing and creating family groups via the groups API.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Returns all groups the current user belongs to.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Group summaries including the caller's role in each group.</returns>
    Task<IReadOnlyList<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new group with the current user as admin.
    /// </summary>
    /// <param name="name">Display name for the new group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created group summary including invite code.</returns>
    Task<GroupSummary> CreateAsync(string name, CancellationToken cancellationToken = default);
}
