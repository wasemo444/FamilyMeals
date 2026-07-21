using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;
using ManageFamilyMeals.Shared.Services;

namespace ManageFamilyMeals.Web.Client.Services;

public sealed class LinkPreviewClient(IHttpClientFactory httpClientFactory) : ILinkPreviewClient
{
    private HttpClient Http => httpClientFactory.CreateClient("MealDataApi");

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
