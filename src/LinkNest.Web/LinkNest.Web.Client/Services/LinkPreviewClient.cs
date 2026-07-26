using System.Net.Http.Json;
using LinkNest.Shared.Models;
using LinkNest.Shared.Services;

namespace LinkNest.Web.Client.Services;

/// <summary>
/// HTTP client wrapper that fetches Open Graph link preview metadata from the API.
/// </summary>
public sealed class LinkPreviewClient(IHttpClientFactory httpClientFactory) : ILinkPreviewClient
{
    private HttpClient Http => httpClientFactory.CreateClient("LinkNestApi");

    public async Task<LinkPreviewData?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync(
            $"/api/link-preview?url={Uri.EscapeDataString(url)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LinkPreviewData>(cancellationToken);
    }
}
