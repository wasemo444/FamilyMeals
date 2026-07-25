using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public interface IOwnershipAuthorization
{
    Task<IReadOnlySet<Guid>> GetMemberGroupIdsAsync(CancellationToken cancellationToken = default);

    Task ValidateCreateOwnerAsync(ContentOwner owner, CancellationToken cancellationToken = default);

    Task EnsureCanMutateCategoryAsync(MealCategory category, CancellationToken cancellationToken = default);

    Task EnsureCanMutateLinkAsync(MealLink link, CancellationToken cancellationToken = default);
}
