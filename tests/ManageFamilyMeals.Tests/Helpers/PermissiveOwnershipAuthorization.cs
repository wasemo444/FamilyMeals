using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Tests.Helpers;

internal sealed class PermissiveOwnershipAuthorization : IOwnershipAuthorization
{
    public Task<IReadOnlySet<Guid>> GetMemberGroupIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

    public Task ValidateCreateOwnerAsync(ContentOwner owner, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EnsureCanMutateCategoryAsync(MealCategory category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EnsureCanMutateLinkAsync(MealLink link, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
