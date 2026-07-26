namespace LinkNest.Shared.Models;

/// <summary>
/// Represents a named container for meal links, scoped to a personal user or a shared group.
/// Categories support soft-delete archival, favorites, and optimistic concurrency via <see cref="RowVersion"/>.
/// </summary>
public sealed class ContentCategory
{
    /// <summary>Unique identifier assigned when the category is created.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name; uniqueness is enforced per owner scope (user or group).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When <see langword="true"/>, the category appears in the favorites section on the home page.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>UTC timestamp recorded when the category was created.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When <see langword="true"/>, the category is soft-deleted and eligible for archive purge.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the category was archived; used for retention policy calculations.</summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>Determines whether this category belongs to an individual user or a group.</summary>
    public OwnerType OwnerType { get; set; } = OwnerType.User;

    /// <summary>User id of the personal owner; set when <see cref="OwnerType"/> is <see cref="OwnerType.User"/>.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Group id when <see cref="OwnerType"/> is <see cref="OwnerType.Group"/>.</summary>
    public Guid? OwnerGroupId { get; set; }

    /// <summary>Denormalized group display name for UI labels; not authoritative for authorization.</summary>
    public string? OwnerGroupName { get; set; }

    /// <summary>Optimistic concurrency token supplied by the persistence layer.</summary>
    public byte[] RowVersion { get; set; } = [0, 0, 0, 0, 0, 0, 0, 1];

    /// <summary>Indicates the category is group-owned and visible to all group members.</summary>
    public bool IsShared => OwnerType == OwnerType.Group;
}
