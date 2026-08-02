using LinkNest.Api.Data;
using LinkNest.Api.Identity;
using LinkNest.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Ensures a default development user exists for local and migrated databases.
/// </summary>
/// <remarks>
/// Skips creation when the well-known user id or email already exists. Refuses the default dev password in production.
/// </remarks>
public sealed class IdentityDataSeeder(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentitySeedOptions> seedOptions,
    IHostEnvironment environment,
    ILogger<IdentityDataSeeder> logger)
{
    /// <summary>
    /// Creates or repairs the default user account when missing or passwordless.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel Identity operations.</param>
    /// <returns>A task that completes when seeding finishes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when production uses the dev password or Identity operations fail.</exception>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var options = seedOptions.Value;

        if (environment.IsProduction()
            && string.Equals(options.DefaultUserPassword, IdentitySeedOptions.DefaultDevPassword, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Skipping default user seed in Production (IdentitySeed:DefaultUserPassword not configured).");
            return;
        }

        var existing = await userManager.FindByIdAsync(WellKnownUsers.DefaultUserId.ToString());
        if (existing is not null)
        {
            if (string.IsNullOrEmpty(existing.PasswordHash))
            {
                var passwordResult = await userManager.AddPasswordAsync(existing, options.DefaultUserPassword);
                if (!passwordResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Failed to set password for default user: {errors}");
                }

                logger.LogInformation(
                    "Set password for default user {Email} created without credentials during migration.",
                    options.DefaultUserEmail);
            }

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
            "Seeded default user {Email} with id {UserId}.",
            options.DefaultUserEmail,
            WellKnownUsers.DefaultUserId);
    }
}
