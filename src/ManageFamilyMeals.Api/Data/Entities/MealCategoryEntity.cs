using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Api.Data.Entities;

/// <summary>
/// Persistence model for a meal category with soft-delete, ownership, and optimistic concurrency.
/// </summary>
/// <remarks>
/// <see cref="RowVersion"/> is an EF concurrency token; clients must send the current value on updates.
/// Ownership is either user-scoped (<see cref="OwnerUserId"/>) or group-scoped (<see cref="OwnerGroupId"/>), enforced by a database check constraint.
/// </remarks>
public sealed class MealCategoryEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsFavorite { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public OwnerType OwnerType { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid? OwnerGroupId { get; set; }

    public byte[] RowVersion { get; set; } = [0, 0, 0, 0, 0, 0, 0, 1];

    public ICollection<MealLinkEntity> Links { get; set; } = [];
}
