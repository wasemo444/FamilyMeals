namespace LinkNest.Shared.Models;

/// <summary>Pending group invite shown to the invited user.</summary>
public sealed class GroupInviteSummary
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string InvitedByDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
