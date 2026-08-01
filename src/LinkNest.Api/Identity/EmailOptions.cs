namespace LinkNest.Api.Identity;

/// <summary>
/// Configuration for outbound transactional email bound from the <c>Email</c> section.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>Configuration section name (<c>Email</c>).</summary>
    public const string SectionName = "Email";

    /// <summary>
    /// When <see langword="true"/>, sends email via SMTP even in Development.
    /// Otherwise Development and Testing log email bodies to the console.
    /// </summary>
    public bool UseSmtp { get; set; }

    /// <summary>SMTP transport settings used by <see cref="SmtpEmailSender"/>.</summary>
    public SmtpOptions Smtp { get; set; } = new();
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
