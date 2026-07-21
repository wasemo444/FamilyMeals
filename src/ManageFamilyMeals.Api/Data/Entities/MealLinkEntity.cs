namespace ManageFamilyMeals.Api.Data.Entities;

public sealed class MealLinkEntity
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public MealCategoryEntity Category { get; set; } = default!;

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
}
