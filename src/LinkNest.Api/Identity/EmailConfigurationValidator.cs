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
        if (!ShouldValidate(options))
        {
            return ValidateOptionsResult.Success;
        }

        if (options.UsesBrevoApi())
        {
            return ValidateBrevoApi(options);
        }

        var errors = GetConfigurationErrors(options.Smtp);
        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static ValidateOptionsResult ValidateBrevoApi(EmailOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BrevoApi.ApiKey))
        {
            errors.Add("Email:BrevoApi:ApiKey must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Smtp.FromAddress))
        {
            errors.Add("Email:Smtp:FromAddress must be configured.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private bool ShouldValidate(EmailOptions options) =>
        options.UseSmtp
        || options.UsesBrevoApi()
        || (!_environment.IsDevelopment()
            && !_environment.IsEnvironment("Testing")
            && !_environment.IsEnvironment("ProductionTesting"));

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
