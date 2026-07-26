using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LinkNest.Shared.Services;

/// <summary>Reads structured error payloads from failed API responses.</summary>
public static class ApiErrorReader
{
    public static async Task<(string? Code, string? Error)> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>(cancellationToken);
            return (payload?.Code, payload?.Error);
        }
        catch
        {
            return (null, null);
        }
    }

    private sealed class ApiErrorPayload
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
