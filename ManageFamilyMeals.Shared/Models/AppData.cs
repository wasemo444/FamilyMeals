namespace ManageFamilyMeals.Shared.Models;

public sealed class AppData
{
    public List<MealCategory> Categories { get; set; } = [];

    public List<MealLink> Links { get; set; } = [];

    public AppSettings Settings { get; set; } = new();
}
