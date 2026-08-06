using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Verifies SMTP connectivity and authentication without sending mail.
/// </summary>
public sealed class SmtpConnectivityChecker(IOptions<EmailOptions> emailOptions)
{
    /// <summary>
    /// Connects to the configured SMTP server and authenticates when credentials are set.
    /// </summary>
    public async Task<SmtpCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var smtp = emailOptions.Value.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            return SmtpCheckResult.Failed("Email:Smtp:Host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(smtp.FromAddress))
        {
            return SmtpCheckResult.Failed("Email:Smtp:FromAddress is not configured.");
        }

        try
        {
            using var client = new SmtpClient
            {
                Timeout = 15_000
            };

            await client.ConnectAsync(
                smtp.Host,
                smtp.Port,
                GetSecureSocketOptions(smtp),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);

            return SmtpCheckResult.Succeeded(smtp.Host, smtp.Port, smtp.FromAddress);
        }
        catch (Exception ex)
        {
            return SmtpCheckResult.Failed(ex.Message, smtp.Host, smtp.Port, smtp.FromAddress);
        }
    }

    private static SecureSocketOptions GetSecureSocketOptions(SmtpOptions smtp) =>
        smtp.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : smtp.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
}

/// <summary>
/// Outcome of an SMTP connectivity check.
/// </summary>
public sealed record SmtpCheckResult(
    bool Ok,
    string Message,
    string? Host = null,
    int? Port = null,
    string? FromAddress = null)
{
    public static SmtpCheckResult Succeeded(string host, int port, string fromAddress) =>
        new(true, "SMTP connection and authentication succeeded.", host, port, fromAddress);

    public static SmtpCheckResult Failed(
        string message,
        string? host = null,
        int? port = null,
        string? fromAddress = null) =>
        new(false, message, host, port, fromAddress);
}
