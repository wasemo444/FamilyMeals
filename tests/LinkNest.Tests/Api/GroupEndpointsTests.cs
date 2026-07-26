using System.Net;
using System.Net.Http.Json;
using LinkNest.Api.Data;
using LinkNest.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Tests.Api;

public class GroupEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public GroupEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateGroup_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/groups", new { name = "Family" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithValidName_CreatorBecomesAdmin()
    {
        using var client = await _factory.CreateFreshAuthenticatedClientAsync();
        var groupName = $"Family-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/groups", new { name = groupName });
        var summary = await response.Content.ReadFromJsonAsync<GroupSummary>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(summary);
        Assert.Equal(groupName, summary!.Name);
        Assert.Equal(GroupRole.Admin, summary.CurrentUserRole);
        Assert.False(string.IsNullOrWhiteSpace(summary.InviteCode));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membership = await dbContext.GroupMemberships
            .SingleAsync(item => item.GroupId == summary.Id);

        Assert.Equal(GroupRole.Admin, membership.Role);
    }

    [Fact]
    public async Task CreateGroup_WithEmptyName_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/groups", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListGroups_ReturnsGroupsForCurrentUser()
    {
        using var client = await _factory.CreateFreshAuthenticatedClientAsync();
        var groupName = $"Family-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/groups", new { name = groupName });

        var groups = await client.GetFromJsonAsync<List<GroupSummary>>("/api/groups");

        Assert.NotNull(groups);
        Assert.Contains(groups!, group => group.Name == groupName && group.CurrentUserRole == GroupRole.Admin);
    }
}
