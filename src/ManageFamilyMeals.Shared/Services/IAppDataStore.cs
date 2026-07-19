using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public interface IAppDataStore
{
    Task<AppData?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppData data, CancellationToken cancellationToken = default);
}
