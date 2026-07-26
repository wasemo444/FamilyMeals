using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// Application-facing contract for reading and mutating meal categories, links, and settings.
/// Implemented by local persistence (<see cref="ContentDataService"/>) and remote API (<see cref="ApiContentDataService"/>).
/// </summary>
public interface IContentDataService
{
    /// <summary>Raised after any mutation so UI components can refresh bound data.</summary>
    event Action? DataChanged;

    /// <summary>
    /// Loads data on first use; equivalent to <see cref="EnsureLoadedAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the in-memory data set is loaded before queries or mutations proceed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads categories and links from the server (or store) after membership changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs startup maintenance such as legacy migration and expired archive purge.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>No-op in <see cref="ApiContentDataService"/>; maintenance runs server-side.</remarks>
    Task RunMaintenanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a detached copy of the current data for export or diagnostics.
    /// </summary>
    /// <returns>Deep copy of categories, links, and settings.</returns>
    AppData GetSnapshot();

    /// <summary>
    /// Updates in-memory settings without persisting until the next explicit save path.
    /// </summary>
    /// <param name="settings">New settings values to apply.</param>
    void ApplySettings(AppSettings settings);

    /// <summary>
    /// Returns non-archived favorite categories, optionally filtered by ownership.
    /// </summary>
    /// <param name="filter">Personal, shared, or all categories.</param>
    IReadOnlyList<ContentCategory> GetFavoriteCategories(HomeContentFilter filter = HomeContentFilter.All);

    /// <summary>
    /// Returns non-archived categories, optionally filtered by ownership.
    /// </summary>
    /// <param name="filter">Personal, shared, or all categories.</param>
    IReadOnlyList<ContentCategory> GetActiveCategories(HomeContentFilter filter = HomeContentFilter.All);

    /// <summary>Returns archived categories ordered by most recently deleted first.</summary>
    IReadOnlyList<ContentCategory> GetArchivedCategories();

    /// <summary>
    /// Finds an active (non-archived) category by id.
    /// </summary>
    /// <param name="categoryId">Category identifier.</param>
    /// <returns>The category, or <see langword="null"/> when not found or archived.</returns>
    ContentCategory? GetCategory(Guid categoryId);

    /// <summary>
    /// Finds a link by id regardless of archive state.
    /// </summary>
    /// <param name="linkId">Link identifier.</param>
    /// <returns>The link, or <see langword="null"/> when not found.</returns>
    SavedLink? GetLink(Guid linkId);

    /// <summary>Returns the current user settings.</summary>
    AppSettings GetSettings();

    /// <summary>
    /// Checks whether a category name is already taken within the given ownership scope.
    /// </summary>
    /// <param name="name">Proposed category name.</param>
    /// <param name="owner">Ownership scope for uniqueness comparison.</param>
    bool IsCategoryNameTaken(string name, ContentOwner owner);

    /// <summary>
    /// Creates a new category under the specified ownership scope.
    /// </summary>
    /// <param name="name">Display name; trimmed before validation.</param>
    /// <param name="owner">Personal or group ownership target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted category.</returns>
    /// <exception cref="InvalidOperationException">A category with the same name already exists in that scope.</exception>
    /// <exception cref="UnauthorizedAccessException">The user cannot create content for the requested group.</exception>
    Task<ContentCategory> AddCategoryAsync(
        string name,
        ContentOwner owner,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a category and all of its active links.
    /// </summary>
    /// <param name="categoryId">Category to archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when archived; <see langword="false"/> when not found or unauthorized.</returns>
    Task<bool> ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived category and all of its archived links.
    /// </summary>
    /// <param name="categoryId">Category to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when restored; <see langword="false"/> when not found or unauthorized.</returns>
    Task<bool> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Toggles the favorite flag on an active category.</summary>
    /// <param name="categoryId">Category to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">The user cannot modify this category.</exception>
    Task ToggleCategoryFavoriteAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Returns favorite active links within a category.</summary>
    /// <param name="categoryId">Parent category id.</param>
    IReadOnlyList<SavedLink> GetFavoriteLinks(Guid categoryId);

    /// <summary>Returns all active links within a category.</summary>
    /// <param name="categoryId">Parent category id.</param>
    IReadOnlyList<SavedLink> GetActiveLinks(Guid categoryId);

    /// <summary>Returns archived links within a category.</summary>
    /// <param name="categoryId">Parent category id.</param>
    IReadOnlyList<SavedLink> GetArchivedLinks(Guid categoryId);

    /// <summary>Returns all archived links across every category.</summary>
    IReadOnlyList<SavedLink> GetAllArchivedLinks();

    /// <summary>
    /// Adds a link to a category, inheriting ownership from the parent category.
    /// </summary>
    /// <param name="categoryId">Target category id.</param>
    /// <param name="titleEn">English title.</param>
    /// <param name="titleAr">Arabic title.</param>
    /// <param name="url">Target URL.</param>
    /// <param name="note">Optional user note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted link.</returns>
    /// <exception cref="KeyNotFoundException">The category does not exist or is archived.</exception>
    /// <exception cref="UnauthorizedAccessException">The user cannot modify the parent category.</exception>
    Task<SavedLink> AddLinkAsync(
        Guid categoryId,
        string titleEn,
        string titleAr,
        string url,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a single link.
    /// </summary>
    /// <param name="linkId">Link to archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when archived; <see langword="false"/> when not found or unauthorized.</returns>
    Task<bool> ArchiveLinkAsync(Guid linkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived link.
    /// </summary>
    /// <param name="linkId">Link to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when restored; <see langword="false"/> when not found or unauthorized.</returns>
    Task<bool> RestoreLinkAsync(Guid linkId, CancellationToken cancellationToken = default);

    /// <summary>Toggles the favorite flag on an active link.</summary>
    /// <param name="linkId">Link to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">The user cannot modify this link.</exception>
    Task ToggleLinkFavoriteAsync(Guid linkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists link-preview metadata and may backfill an empty English title from the preview title.
    /// </summary>
    /// <param name="linkId">Link to update.</param>
    /// <param name="preview">Fetched preview metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateLinkPreviewAsync(Guid linkId, LinkPreviewData preview, CancellationToken cancellationToken = default);
}
