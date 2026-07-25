using ManageFamilyMeals.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ManageFamilyMeals.Tests.Api;

public static class AuthTestHelpers
{
    public static async Task ConfirmEmailAsync(IServiceProvider services, string email)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{email}' was not found.");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to confirm email in test helper.");
        }
    }
}
