namespace LinkNest.Shared.Models;

/// <summary>
/// Open Graph or HTML metadata extracted from a URL, cached on <see cref="SavedLink"/> for display.
/// </summary>
public sealed class LinkPreviewData
{
    /// <summary>Page or article title from preview metadata.</summary>
    public string? Title { get; set; }

    /// <summary>Short description or excerpt from preview metadata.</summary>
    public string? Description { get; set; }

    /// <summary>Representative image URL from preview metadata.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Publisher or site name from preview metadata.</summary>
    public string? SiteName { get; set; }
}
