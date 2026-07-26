namespace LinkNest.Api.Identity;

/// <summary>
/// Development <see cref="IEmailSender"/> implementation that writes email content to the application log.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email sent to {Email}. Subject: {Subject}. Body: {Body}",
            to,
            subject,
            htmlBody);
        return Task.CompletedTask;
    }
}
