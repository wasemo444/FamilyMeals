using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public interface IGroupService
{
    Task<IReadOnlyList<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default);

    Task<GroupSummary> CreateAsync(string name, CancellationToken cancellationToken = default);
}
