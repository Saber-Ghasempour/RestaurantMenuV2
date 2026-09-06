using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class MenuCategoryReadTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MenuCategoryReadTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task GetMenuCategoryShouldReturnCategory()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Read Category Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Drinks",
            5);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content
                .ReadFromJsonAsync<MenuCategoryResponse>();
        Assert.NotNull(content);
        Assert.Equal(category.Id.Value, content.Id);
        Assert.Equal(restaurant.Id.Value, content.RestaurantId);
        Assert.Null(content.ParentCategoryId);
        Assert.Equal("Drinks", content.Name);
        Assert.Equal(5, content.DisplayOrder);
        Assert.Equal(1, content.Version);
    }

    [Fact]
    public async Task GetMenuCategoryShouldNotExposeAnotherRestaurantsCategory()
    {
        await MigrateDatabasesAsync();
        var owner = await _factory.SeedRestaurantAsync(
            "Category Owner",
            DateTimeOffset.UtcNow);
        var otherRestaurant = await _factory.SeedRestaurantAsync(
            "Other Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            owner.Id.Value,
            "Private Category",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{otherRestaurant.Id.Value}/categories/{category.Id.Value}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.CategoryNotFound", problem.Title);
    }

    [Fact]
    public async Task ListMenuCategoriesShouldReturnDeterministicFlatHierarchy()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Category List Restaurant",
            DateTimeOffset.UtcNow);
        var parent = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Main Courses",
            10);
        var child = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Pasta",
            20,
            parent.Id);
        var first = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Appetizers",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content
                .ReadFromJsonAsync<MenuCategoryResponse[]>();
        Assert.NotNull(content);
        Assert.Collection(
            content,
            item => Assert.Equal(first.Id.Value, item.Id),
            item => Assert.Equal(parent.Id.Value, item.Id),
            item =>
            {
                Assert.Equal(child.Id.Value, item.Id);
                Assert.Equal(parent.Id.Value, item.ParentCategoryId);
            });
    }

    [Fact]
    public async Task ListMenuCategoriesShouldReturnNotFoundForUnknownRestaurant()
    {
        await MigrateDatabasesAsync();
        var restaurantId = Guid.CreateVersion7();
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurantId}/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.RestaurantNotFound", problem.Title);
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }

    private sealed record MenuCategoryResponse(
        Guid Id,
        Guid RestaurantId,
        Guid? ParentCategoryId,
        string Name,
        int DisplayOrder,
        DateTimeOffset CreatedAtUtc,
        long Version);

    private sealed record ProblemResponse(
        int Status,
        string Title,
        string Detail);
}
