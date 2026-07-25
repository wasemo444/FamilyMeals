using ManageFamilyMeals.Api.Data;
using ManageFamilyMeals.Api.Identity;
using ManageFamilyMeals.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ManageFamilyMeals.Api.Identity;

public sealed class IdentityDataSeeder(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentitySeedOptions> seedOptions,
    IHostEnvironment environment,
    ILogger<IdentityDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var options = seedOptions.Value;

        if (environment.IsProduction()
            && string.Equals(options.DefaultUserPassword, IdentitySeedOptions.DefaultDevPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Default dev seed password cannot be used in Production. " +
                "Set IdentitySeed:DefaultUserPassword via environment or user secrets.");
        }

        var existing = await userManager.FindByIdAsync(WellKnownUsers.DefaultUserId.ToString());
        if (existing is not null)
        {
            return;
        }

        var userByEmail = await userManager.FindByEmailAsync(options.DefaultUserEmail);
        if (userByEmail is not null)
        {
            logger.LogInformation(
                "Default user email {Email} already exists with id {UserId}.",
                options.DefaultUserEmail,
                userByEmail.Id);
            return;
        }

        var user = new ApplicationUser
        {
            Id = WellKnownUsers.DefaultUserId,
            UserName = options.DefaultUserEmail,
            Email = options.DefaultUserEmail,
            EmailConfirmed = true,
            DisplayName = options.DefaultUserDisplayName,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, options.DefaultUserPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to seed default user: {errors}");
        }

        logger.LogInformation(
            "Seeded default user {Email} with id {UserId}. " +
            "E2 bridge: meal data has no ownership columns yet; all authenticated users share the global dataset until E3.",
            options.DefaultUserEmail,
            WellKnownUsers.DefaultUserId);
    }
}
