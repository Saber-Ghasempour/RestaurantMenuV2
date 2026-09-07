using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class CreateMenuCategoryTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CreateMenuCategoryTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task CreateMenuCategoryShouldReturnCreated()
    {
        await MigrateDatabasesAsync();
        var restaurant =
            await _factory.SeedRestaurantAsync(
                "Catalog Restaurant",
                DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();
        var request = new CreateMenuCategoryRequest(
            null,
            "  Main Courses  ",
            10);

        using var response =
            await client.PostAsJsonAsync(
                $"/api/restaurants/{restaurant.Id.Value}/categories",
                request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<CreateMenuCategoryResponse>();

        Assert.NotNull(content);
        Assert.NotEqual(Guid.Empty, content.Id);
        Assert.Equal(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{content.Id}",
            response.Headers.Location?.OriginalString);

        var persistedCategory =
            await _factory.FindMenuCategoryAsync(content.Id);

        Assert.NotNull(persistedCategory);
        Assert.Equal(restaurant.Id.Value, persistedCategory.RestaurantId);
        Assert.Null(persistedCategory.ParentId);
        Assert.Equal("Main Courses", persistedCategory.Name);
        Assert.Equal(10, persistedCategory.DisplayOrder);
        Assert.Equal(1, persistedCategory.Version);
    }

    [Fact]
    public async Task CreateMenuCategoryShouldReturnForbiddenWithoutMembership()
    {
        await MigrateDatabasesAsync();
        var restaurantId = Guid.CreateVersion7();
        var categoryCountBeforeRequest =
            await _factory.CountMenuCategoriesAsync();
        using var client = _factory.CreateClient();
        var request = new CreateMenuCategoryRequest(
            null,
            "Desserts",
            20);

        using var response =
            await client.PostAsJsonAsync(
                $"/api/restaurants/{restaurantId}/categories",
                request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
        Assert.Equal("Forbidden", problem.Title);
        Assert.Equal(
            categoryCountBeforeRequest,
            await _factory.CountMenuCategoriesAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateMenuCategoryShouldRejectMissingName(
        string? name)
    {
        await MigrateDatabasesAsync();
        var restaurant =
            await _factory.SeedRestaurantAsync(
                "Validation Restaurant",
                DateTimeOffset.UtcNow);
        var categoryCountBeforeRequest =
            await _factory.CountMenuCategoriesAsync();
        using var client = _factory.CreateClient();
        var request = new CreateMenuCategoryRequest(
            null,
            name,
            1);

        using var response =
            await client.PostAsJsonAsync(
                $"/api/restaurants/{restaurant.Id.Value}/categories",
                request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.True(
            problem.Errors.ContainsKey(
                "Catalog.CategoryNameRequired"));
        Assert.Equal(
            categoryCountBeforeRequest,
            await _factory.CountMenuCategoriesAsync());
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }

    private sealed record CreateMenuCategoryRequest(
        Guid? ParentCategoryId,
        string? Name,
        int DisplayOrder);

    private sealed record CreateMenuCategoryResponse(Guid Id);

    private sealed record ProblemResponse(
        int Status,
        string Title,
        string Detail);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors);
}
