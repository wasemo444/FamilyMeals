using Microsoft.AspNetCore.Identity;

namespace ManageFamilyMeals.Api.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
