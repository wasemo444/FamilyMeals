using System.Net;
using System.Net.Http.Json;
using LinkNest.Shared.Auth;
using LinkNest.Shared.Models;

namespace LinkNest.Tests.Api;

public class SharingEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SharingEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GroupMember_SeesSharedCategoryOnBootstrap()
    {
        var memberOneEmail = $"member-one-{Guid.NewGuid():N}@example.com";
        var memberTwoEmail = $"member-two-{Guid.NewGuid():N}@example.com";
        const string password = "RegisterPass1!";

        using var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = memberOneEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Member One"
        });
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = memberTwoEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Member Two"
        });
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, memberOneEmail);
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, memberTwoEmail);

        using var memberOneClient = await _factory.CreateAuthenticatedClientAsync(memberOneEmail, password);
        using var memberTwoClient = await _factory.CreateAuthenticatedClientAsync(memberTwoEmail, password);

        var groupResponse = await memberOneClient.PostAsJsonAsync("/api/groups", new { name = "Family Meals" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();
        Assert.NotNull(group);

        var categoryName = $"Shared-{Guid.NewGuid():N}";
        var createResponse = await memberOneClient.PostAsJsonAsync("/api/categories", new
        {
            name = categoryName,
            ownerType = OwnerType.Group,
            ownerGroupId = group!.Id
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContentCategory>();
        Assert.NotNull(created);
        Assert.Equal(OwnerType.Group, created!.OwnerType);
        Assert.Equal(group.Id, created.OwnerGroupId);

        await AuthTestHelpers.AddGroupMemberAsync(_factory.Services, group.Id, memberTwoEmail);

        var memberOneCategories = await memberOneClient.GetFromJsonAsync<List<ContentCategory>>("/api/categories");
        Assert.NotNull(memberOneCategories);
        Assert.Contains(memberOneCategories!, category => category.Name == categoryName);

        var memberTwoCategories = await memberTwoClient.GetFromJsonAsync<List<ContentCategory>>("/api/categories");
        Assert.NotNull(memberTwoCategories);
        Assert.Contains(memberTwoCategories!, category => category.Name == categoryName);
    }

    [Fact]
    public async Task NonMember_CannotArchiveSharedCategory()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var outsiderEmail = $"outsider-{Guid.NewGuid():N}@example.com";
        const string password = "RegisterPass1!";

        using var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ownerEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Owner"
        });
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = outsiderEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Outsider"
        });
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, ownerEmail);
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, outsiderEmail);

        using var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, password);
        using var outsiderClient = await _factory.CreateAuthenticatedClientAsync(outsiderEmail, password);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/groups", new { name = "Private Group" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();

        var createResponse = await ownerClient.PostAsJsonAsync("/api/categories", new
        {
            name = $"Protected-{Guid.NewGuid():N}",
            ownerType = OwnerType.Group,
            ownerGroupId = group!.Id
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContentCategory>();
        Assert.NotNull(created);

        var archiveResponse = await outsiderClient.PostAsync($"/api/categories/{created!.Id}/archive", null);

        Assert.Equal(HttpStatusCode.NotFound, archiveResponse.StatusCode);
    }

    [Fact]
    public async Task NonMember_CannotCreateGroupCategory()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var outsiderEmail = $"outsider-{Guid.NewGuid():N}@example.com";
        const string password = "RegisterPass1!";

        using var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ownerEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Owner"
        });
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = outsiderEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Outsider"
        });
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, ownerEmail);
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, outsiderEmail);

        using var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, password);
        using var outsiderClient = await _factory.CreateAuthenticatedClientAsync(outsiderEmail, password);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/groups", new { name = "Private Group" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();

        var createResponse = await outsiderClient.PostAsJsonAsync("/api/categories", new
        {
            name = $"Blocked-{Guid.NewGuid():N}",
            ownerType = OwnerType.Group,
            ownerGroupId = group!.Id
        });

        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithGroupOwnerTypeWithoutGroupId_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/categories", new
        {
            name = $"Invalid-{Guid.NewGuid():N}",
            ownerType = OwnerType.Group
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NameAvailable_ForNonMemberGroup_ReturnsNotFound()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var outsiderEmail = $"outsider-{Guid.NewGuid():N}@example.com";
        const string password = "RegisterPass1!";

        using var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ownerEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Owner"
        });
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = outsiderEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Outsider"
        });
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, ownerEmail);
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, outsiderEmail);

        using var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, password);
        using var outsiderClient = await _factory.CreateAuthenticatedClientAsync(outsiderEmail, password);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/groups", new { name = "Private Group" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();

        var response = await outsiderClient.GetAsync(
            $"/api/categories/name-available?name=Breakfast&ownerType={OwnerType.Group}&ownerGroupId={group!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GroupMember_CanEditAnotherMembersSharedCategory()
    {
        var creatorEmail = $"creator-{Guid.NewGuid():N}@example.com";
        var editorEmail = $"editor-{Guid.NewGuid():N}@example.com";
        const string password = "RegisterPass1!";

        using var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = creatorEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Creator"
        });
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = editorEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Editor"
        });
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, creatorEmail);
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, editorEmail);

        using var creatorClient = await _factory.CreateAuthenticatedClientAsync(creatorEmail, password);
        using var editorClient = await _factory.CreateAuthenticatedClientAsync(editorEmail, password);

        var groupResponse = await creatorClient.PostAsJsonAsync("/api/groups", new { name = "Editors Group" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();

        var createResponse = await creatorClient.PostAsJsonAsync("/api/categories", new
        {
            name = $"Team-{Guid.NewGuid():N}",
            ownerType = OwnerType.Group,
            ownerGroupId = group!.Id
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContentCategory>();
        Assert.NotNull(created);

        await AuthTestHelpers.AddGroupMemberAsync(_factory.Services, group!.Id, editorEmail);

        var favoriteResponse = await editorClient.PostAsync($"/api/categories/{created!.Id}/favorite", null);

        favoriteResponse.EnsureSuccessStatusCode();
        var updated = await favoriteResponse.Content.ReadFromJsonAsync<ContentCategory>();
        Assert.NotNull(updated);
        Assert.True(updated!.IsFavorite);
    }

    [Fact]
    public async Task CreateGroupCategory_WithOwnerTypeGroup_SucceedsForMember()
    {
        using var client = await _factory.CreateFreshAuthenticatedClientAsync();
        var groupResponse = await client.PostAsJsonAsync("/api/groups", new { name = $"Group-{Guid.NewGuid():N}" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();
        Assert.NotNull(group);

        var categoryName = $"GroupCat-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/categories", new
        {
            name = categoryName,
            ownerType = OwnerType.Group,
            ownerGroupId = group!.Id
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContentCategory>();
        Assert.NotNull(created);
        Assert.Equal(OwnerType.Group, created!.OwnerType);
        Assert.Equal(group.Id, created.OwnerGroupId);
        Assert.Equal(categoryName, created.Name);
    }

    [Fact]
    public async Task CategoryName_AllowsDuplicateAcrossPersonalAndGroupOwners()
    {
        using var client = await _factory.CreateFreshAuthenticatedClientAsync();
        var sharedName = $"Meals-{Guid.NewGuid():N}";

        var personalResponse = await client.PostAsJsonAsync("/api/categories", new { name = sharedName });
        personalResponse.EnsureSuccessStatusCode();

        var groupResponse = await client.PostAsJsonAsync("/api/groups", new { name = $"Group-{Guid.NewGuid():N}" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.Content.ReadFromJsonAsync<GroupSummary>();

        var groupCategoryResponse = await client.PostAsJsonAsync("/api/categories", new
        {
            name = sharedName,
            ownerType = OwnerType.Group,
            ownerGroupId = group!.Id
        });

        groupCategoryResponse.EnsureSuccessStatusCode();
    }
}
