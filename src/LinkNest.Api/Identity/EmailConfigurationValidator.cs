using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Validates <see cref="EmailOptions"/> at startup for non-development environments.
/// </summary>
public sealed class EmailConfigurationValidator : IValidateOptions<EmailOptions>
{
    private readonly IHostEnvironment _environment;

    public EmailConfigurationValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!ShouldValidateSmtp(options))
        {
            return ValidateOptionsResult.Success;
        }

        var errors = GetConfigurationErrors(options.Smtp);
        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private bool ShouldValidateSmtp(EmailOptions options) =>
        options.UseSmtp
        || (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"));

    /// <summary>
    /// Returns human-readable configuration errors for SMTP settings.
    /// </summary>
    public static List<string> GetConfigurationErrors(SmtpOptions smtp)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            errors.Add("Email:Smtp:Host must be configured.");
        }

        if (smtp.Port <= 0)
        {
            errors.Add("Email:Smtp:Port must be a positive port number.");
        }

        if (string.IsNullOrWhiteSpace(smtp.FromAddress))
        {
            errors.Add("Email:Smtp:FromAddress must be configured.");
        }

        return errors;
    }
}
