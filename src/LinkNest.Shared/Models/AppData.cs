namespace LinkNest.Shared.Models;

/// <summary>
/// Root aggregate persisted by <see cref="Services.IAppDataStore"/> and returned by the bootstrap API.
/// Holds all categories, links, and user preferences for a single tenant scope.
/// </summary>
public sealed class AppData
{
    /// <summary>All meal categories, including archived entries.</summary>
    public List<ContentCategory> Categories { get; set; } = [];

    /// <summary>All meal links across every category, including archived entries.</summary>
    public List<SavedLink> Links { get; set; } = [];

    /// <summary>User-specific application settings such as UI culture.</summary>
    public AppSettings Settings { get; set; } = new();
}
