namespace ManageFamilyMeals.Api.Data.Entities;

/// <summary>
/// Persistence model for application-wide settings stored as a single database row.
/// </summary>
public sealed class AppSettingsEntity
{
    public int Id { get; set; }

    public string? CultureCode { get; set; }
}
