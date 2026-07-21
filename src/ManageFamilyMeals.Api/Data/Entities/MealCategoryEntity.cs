namespace ManageFamilyMeals.Api.Data.Entities;

public sealed class MealCategoryEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsFavorite { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<MealLinkEntity> Links { get; set; } = [];
}
