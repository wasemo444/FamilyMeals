using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LinkNest.Api.Identity;

/// <summary>
/// Production <see cref="IEmailSender"/> implementation that delivers HTML email via SMTP (MailKit).
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Recipient email is required.", nameof(to));
        }

        var smtp = emailOptions.Value.Smtp;
        var message = BuildMessage(smtp, to, subject, htmlBody);

        logger.LogInformation("Sending email to {Email} via SMTP host {Host}:{Port}", to, smtp.Host, smtp.Port);

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, GetSecureSocketOptions(smtp), cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public static MimeMessage BuildMessage(SmtpOptions smtp, string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();
        return message;
    }

    private static SecureSocketOptions GetSecureSocketOptions(SmtpOptions smtp) =>
        smtp.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : smtp.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
}
