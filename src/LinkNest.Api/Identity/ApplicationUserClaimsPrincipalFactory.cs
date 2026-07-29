using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Adds LinkNest-specific claims (display name) to authenticated principals.
/// </summary>
public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(userManager, roleManager, optionsAccessor)
{
    /// <inheritdoc />
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            identity.AddClaim(new Claim("DisplayName", user.DisplayName));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        }

        return identity;
    }
}
