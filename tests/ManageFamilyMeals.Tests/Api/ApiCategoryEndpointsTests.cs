using System.Net;
using System.Net.Http.Json;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Tests.Api;

public class ApiCategoryEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ApiCategoryEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCategory_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", new { name = "Breakfast" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostCategory_WithoutBootstrap_PreservesExistingCategories()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var firstCategory = $"Breakfast-{Guid.NewGuid():N}";
        var secondCategory = $"Lunch-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/categories", new { name = firstCategory });

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", new { name = secondCategory });
        var categories = await client.GetFromJsonAsync<List<MealCategory>>("/api/categories");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(categories);
        Assert.Equal(2, categories!.Count);
        Assert.Contains(categories, category => category.Name == firstCategory);
        Assert.Contains(categories, category => category.Name == secondCategory);
    }

    [Fact]
    public async Task ArchiveCategory_ReturnsNotFoundWhenMissing()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync($"/api/categories/{Guid.NewGuid()}/archive", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RestoreCategory_ReturnsNotFoundWhenMissing()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync($"/api/categories/{Guid.NewGuid()}/restore", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddLink_ReturnsNotFoundWhenCategoryMissing()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/categories/{Guid.NewGuid()}/links",
            new { titleEn = "Soup", titleAr = "حساء", url = "https://example.com/soup" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveLink_ReturnsNotFoundWhenMissing()
    {
        // Arrange
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync($"/api/links/{Guid.NewGuid()}/archive", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
