using LinkNest.Api.Identity;
using Microsoft.Extensions.Configuration;

namespace LinkNest.Api.Startup;

/// <summary>
/// Logs effective email configuration at startup to simplify local SMTP debugging.
/// </summary>
internal static class EmailStartupDiagnostics
{
    public static void Log(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
    {
        var useSmtp = configuration.GetValue<bool>($"{EmailOptions.SectionName}:UseSmtp");
        var provider = configuration[$"{EmailOptions.SectionName}:Provider"] ?? EmailProviders.Smtp;
        var brevoApiKeyConfigured = !string.IsNullOrWhiteSpace(
            configuration[$"{EmailOptions.SectionName}:BrevoApi:ApiKey"]);
        var host = configuration[$"{EmailOptions.SectionName}:Smtp:Host"];
        var port = configuration.GetValue<int>($"{EmailOptions.SectionName}:Smtp:Port");
        var username = configuration[$"{EmailOptions.SectionName}:Smtp:Username"];
        var fromAddress = configuration[$"{EmailOptions.SectionName}:Smtp:FromAddress"];
        var passwordConfigured = !string.IsNullOrWhiteSpace(
            configuration[$"{EmailOptions.SectionName}:Smtp:Password"]);

        var useBrevoApi = brevoApiKeyConfigured
            || string.Equals(provider, EmailProviders.BrevoApi, StringComparison.OrdinalIgnoreCase);
        var willUseSmtp = !useBrevoApi
            && (useSmtp || (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")));

        logger.LogInformation(
            "Email config: Provider={Provider}, UseSmtp={UseSmtp}, Environment={Environment}, Host={Host}, Port={Port}, UsernameSet={UsernameSet}, FromAddress={FromAddress}, PasswordSet={PasswordSet}, BrevoApiKeySet={BrevoApiKeySet}, EffectiveMode={Mode}",
            provider,
            useSmtp,
            environment.EnvironmentName,
            string.IsNullOrWhiteSpace(host) ? "(empty)" : host,
            port,
            !string.IsNullOrWhiteSpace(username),
            string.IsNullOrWhiteSpace(fromAddress) ? "(empty)" : fromAddress,
            passwordConfigured,
            brevoApiKeyConfigured,
            useBrevoApi ? "BrevoApi" : willUseSmtp ? "SMTP" : "LogOnly");

        if (willUseSmtp && (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress)))
        {
            logger.LogWarning(
                "SMTP mode is enabled but Email:Smtp:Host or Email:Smtp:FromAddress is missing. Email sends will fail at runtime.");
        }

        if (useBrevoApi && string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogWarning(
                "Brevo API mode is enabled but Email:Smtp:FromAddress is missing. Email sends will fail at runtime.");
        }
    }
}
