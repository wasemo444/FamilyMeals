namespace LinkNest.Api.Identity;

/// <summary>
/// Known outbound email transport providers.
/// </summary>
public static class EmailProviders
{
    public const string Smtp = "Smtp";
    public const string BrevoApi = "BrevoApi";
}

/// <summary>
/// Configuration for outbound transactional email bound from the <c>Email</c> section.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>Configuration section name (<c>Email</c>).</summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Email transport: <see cref="EmailProviders.Smtp"/> (default) or <see cref="EmailProviders.BrevoApi"/>.
    /// When <see cref="BrevoApi"/> is configured with an API key, HTTPS delivery is used instead of SMTP.
    /// </summary>
    public string Provider { get; set; } = EmailProviders.Smtp;

    /// <summary>
    /// When <see langword="true"/>, sends email via SMTP even in Development.
    /// Otherwise Development and Testing log email bodies to the console.
    /// </summary>
    public bool UseSmtp { get; set; }

    /// <summary>SMTP transport settings used by <see cref="SmtpEmailSender"/>.</summary>
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>Brevo REST API settings used by <see cref="BrevoApiEmailSender"/>.</summary>
    public BrevoApiOptions BrevoApi { get; set; } = new();

    /// <summary>
    /// Returns <see langword="true"/> when email should be sent via the Brevo HTTPS API.
    /// </summary>
    public bool UsesBrevoApi() =>
        !string.IsNullOrWhiteSpace(BrevoApi.ApiKey)
        || string.Equals(Provider, EmailProviders.BrevoApi, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// SMTP server settings for sending email via MailKit.
/// </summary>
public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "LinkNest";

    public bool UseStartTls { get; set; } = true;
}

/// <summary>
/// Brevo transactional email API settings (HTTPS — works on hosts that block SMTP ports).
/// </summary>
public sealed class BrevoApiOptions
{
    /// <summary>Brevo API key from Settings → SMTP &amp; API → API Keys (not the SMTP key).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
