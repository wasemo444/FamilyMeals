using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Builds confirmation links and sends email confirmation messages for newly registered users.
/// </summary>
public sealed class EmailConfirmationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<AuthOptions> authOptions)
{
    /// <summary>
    /// Generates a confirmation token and sends a confirmation email to the user.
    /// </summary>
    /// <param name="user">The user to confirm.</param>
    /// <param name="cancellationToken">Token used to cancel the send operation.</param>
    /// <returns>A task that completes when the email has been dispatched.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user has no email or username.</exception>
    public Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken = default) =>
        SendConfirmationEmailInternalAsync(user, cancellationToken);

    /// <summary>
    /// Resends a confirmation email when the account exists, is active, and is not yet confirmed.
    /// </summary>
    public async Task ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive || user.EmailConfirmed)
        {
            return;
        }

        await SendConfirmationEmailInternalAsync(user, cancellationToken);
    }

    private async Task SendConfirmationEmailInternalAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var email = user.Email ?? user.UserName
            ?? throw new InvalidOperationException("User email is required to send confirmation.");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = BuildConfirmationLink(user.Id, token);

        await emailSender.SendEmailAsync(
            email,
            "Confirm your LinkNest account",
            $"""
            <p>Thanks for registering.</p>
            <p>Please confirm your email by clicking this link:</p>
            <p><a href="{confirmationLink}">{confirmationLink}</a></p>
            """,
            cancellationToken);
    }

    /// <summary>
    /// Builds the absolute confirmation URL pointing at the static web client's confirm-email page.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="token">The URL-encoded confirmation token from Identity.</param>
    /// <returns>The full confirmation link.</returns>
    public string BuildConfirmationLink(Guid userId, string token)
    {
        var webBaseUrl = authOptions.Value.WebBaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(token);
        return $"{webBaseUrl}/confirm-email?userId={userId}&code={encodedToken}";
    }
}
