namespace ManageFamilyMeals.Api.Identity;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
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
