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
        var host = configuration[$"{EmailOptions.SectionName}:Smtp:Host"];
        var port = configuration.GetValue<int>($"{EmailOptions.SectionName}:Smtp:Port");
        var username = configuration[$"{EmailOptions.SectionName}:Smtp:Username"];
        var fromAddress = configuration[$"{EmailOptions.SectionName}:Smtp:FromAddress"];
        var passwordConfigured = !string.IsNullOrWhiteSpace(
            configuration[$"{EmailOptions.SectionName}:Smtp:Password"]);

        var willUseSmtp = useSmtp
            || (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"));

        logger.LogInformation(
            "Email config: UseSmtp={UseSmtp}, Environment={Environment}, Host={Host}, Port={Port}, UsernameSet={UsernameSet}, FromAddress={FromAddress}, PasswordSet={PasswordSet}, EffectiveMode={Mode}",
            useSmtp,
            environment.EnvironmentName,
            string.IsNullOrWhiteSpace(host) ? "(empty)" : host,
            port,
            !string.IsNullOrWhiteSpace(username),
            string.IsNullOrWhiteSpace(fromAddress) ? "(empty)" : fromAddress,
            passwordConfigured,
            willUseSmtp ? "SMTP" : "LogOnly");

        if (willUseSmtp && (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress)))
        {
            logger.LogWarning(
                "SMTP mode is enabled but Email:Smtp:Host or Email:Smtp:FromAddress is missing. Email sends will fail at runtime.");
        }
    }
}
