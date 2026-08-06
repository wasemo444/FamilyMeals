using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Verifies Brevo API credentials without sending email.
/// </summary>
public sealed class BrevoApiConnectivityChecker(
    HttpClient httpClient,
    IOptions<EmailOptions> emailOptions)
{
    /// <summary>
    /// Calls Brevo <c>GET /v3/account</c> to validate the configured API key.
    /// </summary>
    public async Task<SmtpCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var options = emailOptions.Value;
        var apiKey = options.BrevoApi.ApiKey;
        var fromAddress = options.Smtp.FromAddress;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return SmtpCheckResult.Failed("Email:BrevoApi:ApiKey is not configured.", fromAddress: fromAddress);
        }

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            return SmtpCheckResult.Failed("Email:Smtp:FromAddress is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "v3/account");
            request.Headers.TryAddWithoutValidation("api-key", apiKey);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return SmtpCheckResult.Failed(
                    $"Brevo API returned {(int)response.StatusCode}: {body}",
                    fromAddress: fromAddress);
            }

            return SmtpCheckResult.Succeeded("api.brevo.com", 443, fromAddress);
        }
        catch (Exception ex)
        {
            return SmtpCheckResult.Failed(ex.Message, fromAddress: fromAddress);
        }
    }
}
