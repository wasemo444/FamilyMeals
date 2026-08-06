using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LinkNest.Api.Identity;

/// <summary>
/// Sends transactional email via the Brevo REST API over HTTPS (port 443).
/// Use this on hosts that block outbound SMTP, such as Render free tier.
/// </summary>
public sealed class BrevoApiEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> emailOptions,
    ILogger<BrevoApiEmailSender> logger) : IEmailSender
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

        var options = emailOptions.Value;
        var apiKey = options.BrevoApi.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Email:BrevoApi:ApiKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Smtp.FromAddress))
        {
            throw new InvalidOperationException("Email:Smtp:FromAddress is required for the Brevo sender address.");
        }

        var payload = new BrevoSendEmailRequest
        {
            Sender = new BrevoEmailParty(options.Smtp.FromAddress, options.Smtp.FromName),
            To = [new BrevoEmailParty(to)],
            Subject = subject,
            HtmlContent = htmlBody
        };

        logger.LogInformation(
            "Sending email to {Email} via Brevo API (from {FromAddress})",
            to,
            options.Smtp.FromAddress);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("api-key", apiKey);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Brevo API send failed for {Email} with status {StatusCode}: {Body}",
                to,
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"Brevo API returned {(int)response.StatusCode}: {TryReadMessage(body) ?? "email send failed"}");
        }

        logger.LogInformation("Email delivered to {Email} with subject {Subject}", to, subject);
    }

    private static string? TryReadMessage(string body)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize<BrevoErrorResponse>(body);
            return payload?.Message;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed class BrevoSendEmailRequest
    {
        [JsonPropertyName("sender")]
        public BrevoEmailParty Sender { get; set; } = default!;

        [JsonPropertyName("to")]
        public List<BrevoEmailParty> To { get; set; } = [];

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;
    }

    private sealed record BrevoEmailParty(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string? Name = null);

    private sealed class BrevoErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
