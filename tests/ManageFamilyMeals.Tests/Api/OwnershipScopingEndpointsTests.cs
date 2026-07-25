using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Auth;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Tests.Api;

public class OwnershipScopingEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public OwnershipScopingEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Categories_AreScopedToAuthenticatedUser()
    {
        var userOneEmail = $"user-one-{Guid.NewGuid():N}@example.com";
        var userTwoEmail = $"user-two-{Guid.NewGuid():N}@example.com";
        const string password = "RegisterPass1!";

        using var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = userOneEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "User One"
        });
        await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = userTwoEmail,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "User Two"
        });
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, userOneEmail);
        await AuthTestHelpers.ConfirmEmailAsync(_factory.Services, userTwoEmail);

        using var userOneClient = await _factory.CreateAuthenticatedClientAsync(userOneEmail, password);
        using var userTwoClient = await _factory.CreateAuthenticatedClientAsync(userTwoEmail, password);

        var categoryName = $"Private-{Guid.NewGuid():N}";
        var createResponse = await userOneClient.PostAsJsonAsync("/api/categories", new { name = categoryName });
        createResponse.EnsureSuccessStatusCode();

        var userOneCategories = await userOneClient.GetFromJsonAsync<List<MealCategory>>("/api/categories");
        var userTwoCategories = await userTwoClient.GetFromJsonAsync<List<MealCategory>>("/api/categories");

        Assert.NotNull(userOneCategories);
        Assert.Contains(userOneCategories!, category => category.Name == categoryName);
        Assert.NotNull(userTwoCategories);
        Assert.DoesNotContain(userTwoCategories!, category => category.Name == categoryName);
    }

    [Fact]
    public async Task ConcurrentCategoryUpdates_ReturnConflictForSecondWriter()
    {
        using var clientOne = await _factory.CreateAuthenticatedClientAsync();
        using var clientTwo = await _factory.CreateAuthenticatedClientAsync();

        var categoryName = $"Concurrent-{Guid.NewGuid():N}";
        var createResponse = await clientOne.PostAsJsonAsync("/api/categories", new { name = categoryName });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MealCategory>();
        Assert.NotNull(created);

        var favoriteOne = clientOne.PostAsync($"/api/categories/{created!.Id}/favorite", null);
        var favoriteTwo = clientTwo.PostAsync($"/api/categories/{created.Id}/favorite", null);
        var responses = await Task.WhenAll(favoriteOne, favoriteTwo);
        Assert.Contains(responses, response => response.IsSuccessStatusCode);
        Assert.Contains(responses, response => response.StatusCode == System.Net.HttpStatusCode.Conflict);
    }
}
