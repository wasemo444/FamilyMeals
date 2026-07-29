using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Builds password-reset links and sends reset emails to registered users.
/// </summary>
public sealed class PasswordResetService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<AuthOptions> authOptions)
{
    /// <summary>
    /// Sends a password-reset email when the account exists and is active.
    /// </summary>
    /// <remarks>
    /// Callers should not reveal whether the email was found; this method returns silently when no user matches.
    /// </remarks>
    public async Task SendResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = BuildResetLink(user.Id, token);

        await emailSender.SendEmailAsync(
            normalizedEmail,
            "Reset your LinkNest password",
            $"""
            <p>We received a request to reset your password.</p>
            <p>Click the link below to choose a new password:</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """,
            cancellationToken);
    }

    /// <summary>
    /// Resets the user's password using an Identity reset token.
    /// </summary>
    public async Task<IdentityResult> ResetPasswordAsync(
        Guid userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidToken",
                Description = "Password reset link is invalid or expired."
            });
        }

        return await userManager.ResetPasswordAsync(user, token, newPassword);
    }

    /// <summary>
    /// Builds the absolute reset URL pointing at the web host's reset-password page.
    /// </summary>
    public string BuildResetLink(Guid userId, string token)
    {
        var webBaseUrl = authOptions.Value.WebBaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(token);
        return $"{webBaseUrl}/reset-password?userId={userId}&code={encodedToken}";
    }
}
