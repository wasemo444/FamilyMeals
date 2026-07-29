using Microsoft.AspNetCore.Identity;

namespace LinkNest.Api.Identity;

/// <summary>
/// ASP.NET Identity user entity with display name and creation timestamp.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When false, the account is soft-deactivated and cannot sign in.</summary>
    public bool IsActive { get; set; } = true;
}
