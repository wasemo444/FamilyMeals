using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Web.Client.Services;

public sealed class LinkPreviewClient(HttpClient httpClient) : ILinkPreviewClient
{
    public async Task<LinkPreviewData?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"/api/link-preview?url={Uri.EscapeDataString(url)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LinkPreviewData>(cancellationToken);
    }
}