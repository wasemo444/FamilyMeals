namespace LinkNest.Shared.Models;

/// <summary>Group member row for the members list UI.</summary>
public sealed class GroupMemberSummary
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public GroupRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
