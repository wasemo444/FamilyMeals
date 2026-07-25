using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Shared.Services;

public sealed class GroupClient(IHttpClientFactory httpClientFactory) : IGroupService
{
    private HttpClient Http => httpClientFactory.CreateClient("MealDataApi");

    public async Task<IReadOnlyList<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync("/api/groups", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GroupSummary>>(cancellationToken) ?? [];
    }

    public async Task<GroupSummary> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/groups", new { name }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GroupSummary>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created group.");
    }
}
