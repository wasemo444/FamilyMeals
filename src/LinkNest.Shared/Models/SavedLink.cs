using System.Text.Json.Serialization;

namespace LinkNest.Shared.Models;

/// <summary>
/// Represents a saved URL within a <see cref="ContentCategory"/>, with bilingual titles,
/// optional notes, cached link-preview metadata, and ownership inherited from its parent category.
/// </summary>
public sealed class SavedLink
{
    /// <summary>Unique identifier assigned when the link is created.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent category that owns this link.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>English display title; may be auto-filled from link preview when empty.</summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>Arabic display title; used when the UI culture is Arabic.</summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>Legacy single-title field retained for backward-compatible JSON deserialization.</summary>
    [JsonPropertyName("title")]
    public string? LegacyTitle { get; set; }

    /// <summary>Target URL opened when the user selects the link.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Optional free-text note attached by the user.</summary>
    public string? Note { get; set; }

    /// <summary>When <see langword="true"/>, the link appears in the category favorites section.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>UTC timestamp recorded when the link was created.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When <see langword="true"/>, the link is soft-deleted and eligible for archive purge.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the link was archived.</summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>Title fetched from Open Graph or page metadata.</summary>
    public string? PreviewTitle { get; set; }

    /// <summary>Description fetched from link-preview metadata.</summary>
    public string? PreviewDescription { get; set; }

    /// <summary>Thumbnail or hero image URL from link-preview metadata.</summary>
    public string? PreviewImageUrl { get; set; }

    /// <summary>Site or publisher name from link-preview metadata.</summary>
    public string? PreviewSiteName { get; set; }

    /// <summary>Ownership scope copied from the parent category at creation time.</summary>
    public OwnerType OwnerType { get; set; } = OwnerType.User;

    /// <summary>User id of the personal owner; set when <see cref="OwnerType"/> is <see cref="OwnerType.User"/>.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Group id when <see cref="OwnerType"/> is <see cref="OwnerType.Group"/>.</summary>
    public Guid? OwnerGroupId { get; set; }

    /// <summary>Optimistic concurrency token supplied by the persistence layer.</summary>
    public byte[] RowVersion { get; set; } = [0, 0, 0, 0, 0, 0, 0, 1];
}
