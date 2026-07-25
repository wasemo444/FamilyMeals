namespace ManageFamilyMeals.Shared.Models;

public sealed class MealCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public bool IsFavorite { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public OwnerType OwnerType { get; set; } = OwnerType.User;

    public Guid? OwnerUserId { get; set; }

    public Guid? OwnerGroupId { get; set; }

    public string? OwnerGroupName { get; set; }

    public byte[] RowVersion { get; set; } = [0, 0, 0, 0, 0, 0, 0, 1];

    public bool IsShared => OwnerType == OwnerType.Group;
}
