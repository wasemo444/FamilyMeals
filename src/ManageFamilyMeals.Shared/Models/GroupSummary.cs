namespace ManageFamilyMeals.Shared.Models;

public sealed class GroupSummary
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string InviteCode { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public GroupRole CurrentUserRole { get; set; }
}
