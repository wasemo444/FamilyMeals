using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ManageFamilyMeals.Api.Identity;

public sealed class EmailConfirmationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<AuthOptions> authOptions)
{
    public async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var email = user.Email ?? user.UserName
            ?? throw new InvalidOperationException("User email is required to send confirmation.");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = BuildConfirmationLink(user.Id, token);

        await emailSender.SendEmailAsync(
            email,
            "Confirm your Manage Family Meals account",
            $"""
            <p>Thanks for registering.</p>
            <p>Please confirm your email by clicking this link:</p>
            <p><a href="{confirmationLink}">{confirmationLink}</a></p>
            """,
            cancellationToken);
    }

    public string BuildConfirmationLink(Guid userId, string token)
    {
        var webBaseUrl = authOptions.Value.WebBaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(token);
        return $"{webBaseUrl}/account/confirm-email?userId={userId}&code={encodedToken}";
    }
}
