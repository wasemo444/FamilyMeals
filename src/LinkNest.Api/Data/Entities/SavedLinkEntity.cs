using LinkNest.Shared.Models;

namespace LinkNest.Api.Data.Entities;

/// <summary>
/// Persistence model for a meal link within a category, including preview metadata and ownership.
/// </summary>
/// <remarks>
/// <see cref="RowVersion"/> is an EF concurrency token. Ownership mirrors the parent category or is set explicitly on create/move.
/// </remarks>
public sealed class SavedLinkEntity
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public ContentCategoryEntity Category { get; set; } = default!;

    public string TitleEn { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    public string? LegacyTitle { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? Note { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? PreviewTitle { get; set; }

    public string? PreviewDescription { get; set; }

    public string? PreviewImageUrl { get; set; }

    public string? PreviewSiteName { get; set; }

    public OwnerType OwnerType { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid? OwnerGroupId { get; set; }

    public byte[] RowVersion { get; set; } = [0, 0, 0, 0, 0, 0, 0, 1];
}
