namespace LinkNest.Shared.Models;

/// <summary>Lifecycle state of an email invite to join a group.</summary>
public enum GroupInviteStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2
}
