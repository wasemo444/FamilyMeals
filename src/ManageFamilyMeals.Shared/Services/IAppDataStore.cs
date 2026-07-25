using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

/// <summary>
/// Abstraction over meal-data persistence used by <see cref="MealDataService"/> for load and save operations.
/// </summary>
public interface IAppDataStore
{
    /// <summary>
    /// Loads the full application data aggregate from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored <see cref="AppData"/>, or <see langword="null"/> when no data exists yet.</returns>
    Task<AppData?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the full application data aggregate to storage.
    /// </summary>
    /// <param name="data">Complete snapshot to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(AppData data, CancellationToken cancellationToken = default);
}
