namespace ManageFamilyMeals.Shared.Constants;

public static class ArchivePolicy
{
    public const int RetentionDays = 7;

    public static DateTime ExpirationThresholdUtc =>
        DateTime.UtcNow.AddDays(-RetentionDays);
}
