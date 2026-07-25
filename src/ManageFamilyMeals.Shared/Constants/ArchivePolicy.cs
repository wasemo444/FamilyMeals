namespace ManageFamilyMeals.Shared.Constants;

/// <summary>
/// Retention rules for soft-deleted categories and links held in the archive.
/// </summary>
public static class ArchivePolicy
{
    /// <summary>Number of days archived items are retained before permanent deletion.</summary>
    public const int RetentionDays = 7;

    /// <summary>
    /// UTC cutoff before which archived items are eligible for purge during maintenance.
    /// </summary>
    /// <remarks>Computed from <see cref="RetentionDays"/> relative to the current UTC time.</remarks>
    public static DateTime ExpirationThresholdUtc =>
        DateTime.UtcNow.AddDays(-RetentionDays);
}
