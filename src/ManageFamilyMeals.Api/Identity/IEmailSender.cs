namespace ManageFamilyMeals.Api.Identity;

/// <summary>
/// Abstraction for sending transactional HTML email (for example, address confirmation).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an HTML email to a recipient.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">HTML message body.</param>
    /// <param name="cancellationToken">Token used to cancel the send operation.</param>
    /// <returns>A task that completes when the message has been sent.</returns>
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
