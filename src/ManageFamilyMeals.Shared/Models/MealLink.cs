using System.Text.Json.Serialization;

namespace ManageFamilyMeals.Shared.Models;

public sealed class MealLink
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CategoryId { get; set; }

    public string TitleEn { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? LegacyTitle { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? Note { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? PreviewTitle { get; set; }

    public string? PreviewDescription { get; set; }

    public string? PreviewImageUrl { get; set; }

    public string? PreviewSiteName { get; set; }
}
