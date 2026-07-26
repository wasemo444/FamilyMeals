namespace LinkNest.Shared.Models;

/// <summary>
/// User preferences stored alongside meal data in <see cref="AppData"/>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// BCP 47 culture code (for example, <c>en</c> or <c>ar</c>) driving localized UI strings.
    /// </summary>
    public string? CultureCode { get; set; }
}
