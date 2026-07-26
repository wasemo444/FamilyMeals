using System.Net;
using System.Net.Http.Json;
using LinkNest.Shared.Models;

namespace LinkNest.Shared.Services;

/// <summary>
/// HTTP client implementation of <see cref="IGroupService"/> using the named <c>LinkNestApi</c> client.
/// </summary>
public sealed class GroupClient(IHttpClientFactory httpClientFactory) : IGroupService
{
    private HttpClient Http => httpClientFactory.CreateClient("LinkNestApi");

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupSummary>> GetMyGroupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync("/api/groups", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GroupSummary>>(cancellationToken) ?? [];
    }

    /// <inheritdoc />
    public async Task<GroupSummary> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync("/api/groups", new { name }, cancellationToken);
        await EnsureSuccessOrBadRequestAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<GroupSummary>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created group.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupMemberSummary>> GetMembersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync($"/api/groups/{groupId}/members", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GroupMemberSummary>>(cancellationToken) ?? [];
    }

    /// <inheritdoc />
    public async Task InviteMemberAsync(Guid groupId, string email, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsJsonAsync($"/api/groups/{groupId}/invites", new { email }, cancellationToken);
        await EnsureSuccessOrBadRequestAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupInviteSummary>> GetPendingInvitesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await Http.GetAsync("/api/groups/invites/pending", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GroupInviteSummary>>(cancellationToken) ?? [];
    }

    /// <inheritdoc />
    public async Task AcceptInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/groups/invites/{inviteId}/accept", null, cancellationToken);
        await EnsureSuccessOrBadRequestAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeclineInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/groups/invites/{inviteId}/decline", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await Http.DeleteAsync($"/api/groups/{groupId}/members/{userId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task LeaveGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"/api/groups/{groupId}/leave", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task EnsureSuccessOrBadRequestAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var (code, error) = await ApiErrorReader.ReadAsync(response, cancellationToken);
            throw new ApiBadRequestException(code ?? "bad_request", error);
        }

        response.EnsureSuccessStatusCode();
    }
}
