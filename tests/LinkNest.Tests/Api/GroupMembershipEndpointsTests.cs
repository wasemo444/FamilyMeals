using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinkNest.Api.Data;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Tests.Api;

public class GroupMembershipEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public GroupMembershipEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invite_WhenAdminAndValidEmail_CreatesPendingInvite()
    {
        var inviteeEmail = $"member-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, inviteeEmail);

        using var adminClient = await CreateAdminClientAsync();
        var group = await CreateGroupAsync(adminClient);

        var response = await adminClient.PostAsJsonAsync($"/api/groups/{group.Id}/invites", new { email = inviteeEmail });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invite = await db.GroupInvites.SingleAsync(i => i.GroupId == group.Id);
        Assert.Equal(GroupInviteStatus.Pending, invite.Status);
    }

    [Fact]
    public async Task Invite_WhenEmailNotRegistered_ReturnsBadRequest()
    {
        using var adminClient = await CreateAdminClientAsync();
        var group = await CreateGroupAsync(adminClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/groups/{group.Id}/invites",
            new { email = $"missing-{Guid.NewGuid():N}@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invitee_not_found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Invite_WhenEmailUnconfirmed_ReturnsBadRequest()
    {
        var inviteeEmail = $"unconfirmed-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterUserAsync(_factory.Services, inviteeEmail, confirmEmail: false);

        using var adminClient = await CreateAdminClientAsync();
        var group = await CreateGroupAsync(adminClient);

        var response = await adminClient.PostAsJsonAsync($"/api/groups/{group.Id}/invites", new { email = inviteeEmail });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invitee_email_unconfirmed", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AcceptInvite_CreatesMembershipAndClearsPending()
    {
        var inviteeEmail = $"accept-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, inviteeEmail);

        using var adminClient = await CreateAdminClientAsync();
        var group = await CreateGroupAsync(adminClient);
        await adminClient.PostAsJsonAsync($"/api/groups/{group.Id}/invites", new { email = inviteeEmail });

        using var inviteeClient = await _factory.CreateAuthenticatedClientAsync(inviteeEmail);
        var pending = await inviteeClient.GetFromJsonAsync<List<GroupInviteSummary>>("/api/groups/invites/pending");
        Assert.NotNull(pending);
        Assert.Single(pending!);

        var acceptResponse = await inviteeClient.PostAsync($"/api/groups/invites/{pending![0].Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode();

        var groups = await inviteeClient.GetFromJsonAsync<List<GroupSummary>>("/api/groups");
        Assert.Contains(groups!, g => g.Id == group.Id && g.CurrentUserRole == GroupRole.Member);
    }

    [Fact]
    public async Task Invite_WhenInviteeAlreadyInGroup_ReturnsBadRequest()
    {
        var memberEmail = $"in-group-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, memberEmail);

        using var adminClient1 = await CreateAdminClientAsync();
        var group1 = await CreateGroupAsync(adminClient1);
        await adminClient1.PostAsJsonAsync($"/api/groups/{group1.Id}/invites", new { email = memberEmail });

        using var memberClient = await _factory.CreateAuthenticatedClientAsync(memberEmail);
        var pending = await memberClient.GetFromJsonAsync<List<GroupInviteSummary>>("/api/groups/invites/pending");
        await memberClient.PostAsync($"/api/groups/invites/{pending![0].Id}/accept", null);

        using var adminClient2 = await CreateSecondGroupAdminClientAsync();
        var group2 = await CreateGroupAsync(adminClient2);

        var response = await adminClient2.PostAsJsonAsync($"/api/groups/{group2.Id}/invites", new { email = memberEmail });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invitee_in_group", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateGroup_WhenUserAlreadyInGroup_ReturnsBadRequest()
    {
        using var client = await _factory.CreateFreshAuthenticatedClientAsync();
        await CreateGroupAsync(client);

        var response = await client.PostAsJsonAsync("/api/groups", new { name = "Second Group" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("user_in_group", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RemoveMember_WhenAdmin_RemovesMembershipOnly()
    {
        var memberEmail = $"remove-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, memberEmail);

        using var adminClient = await CreateAdminClientAsync();
        var group = await CreateGroupAsync(adminClient);
        await adminClient.PostAsJsonAsync($"/api/groups/{group.Id}/invites", new { email = memberEmail });

        using var memberClient = await _factory.CreateAuthenticatedClientAsync(memberEmail);
        var pending = await memberClient.GetFromJsonAsync<List<GroupInviteSummary>>("/api/groups/invites/pending");
        await memberClient.PostAsync($"/api/groups/invites/{pending![0].Id}/accept", null);

        var memberId = (await memberClient.GetFromJsonAsync<AuthUserInfo>("/api/auth/me"))!.Id;
        var response = await adminClient.DeleteAsync($"/api/groups/{group.Id}/members/{memberId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.GroupMemberships.AnyAsync(m => m.GroupId == group.Id && m.UserId == memberId));
    }

    [Fact]
    public async Task ListMembers_WhenMember_ReturnsMemberList()
    {
        using var adminClient = await CreateAdminClientAsync();
        var group = await CreateGroupAsync(adminClient);

        var members = await adminClient.GetFromJsonAsync<List<GroupMemberSummary>>($"/api/groups/{group.Id}/members");

        Assert.NotNull(members);
        Assert.Single(members!);
        Assert.Equal(GroupRole.Admin, members![0].Role);
    }

    private async Task<HttpClient> CreateAdminClientAsync() =>
        await _factory.CreateFreshAuthenticatedClientAsync();

    private static async Task<GroupSummary> CreateGroupAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/groups", new { name = $"Group-{Guid.NewGuid():N}" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupSummary>())!;
    }

    private async Task<HttpClient> CreateSecondGroupAdminClientAsync()
    {
        var email = $"admin2-{Guid.NewGuid():N}@example.com";
        await AuthTestHelpers.RegisterAndConfirmUserAsync(_factory.Services, email);
        return await _factory.CreateAuthenticatedClientAsync(email);
    }
}
