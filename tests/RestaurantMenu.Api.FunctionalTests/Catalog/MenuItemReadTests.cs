using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class MenuItemReadTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MenuItemReadTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task GetMenuItemShouldReturnProjectedMoneyAndMetadata()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Read Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Main Courses",
            1);
        var item = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Carbonara",
            14.50m,
            "EUR",
            10);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{item.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content.ReadFromJsonAsync<MenuItemResponse>();
        Assert.NotNull(content);
        Assert.Equal(item.Id.Value, content.Id);
        Assert.Equal(restaurant.Id.Value, content.RestaurantId);
        Assert.Equal(category.Id.Value, content.CategoryId);
        Assert.Equal("Carbonara", content.Name);
        Assert.Equal(14.50m, content.PriceAmount);
        Assert.Equal("EUR", content.Currency);
        Assert.True(content.IsAvailable);
        Assert.Equal(1, content.Version);
    }

    [Fact]
    public async Task GetMenuItemShouldNotExposeItemThroughAnotherCategory()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Scoped Item Restaurant",
            DateTimeOffset.UtcNow);
        var ownerCategory = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Owner",
            1);
        var otherCategory = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Other",
            2);
        var item = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            ownerCategory.Id,
            "Scoped Item",
            10m,
            "EUR",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{otherCategory.Id.Value}/items/{item.Id.Value}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.MenuItemNotFound", problem.Title);
    }

    [Fact]
    public async Task ListMenuItemsShouldReturnOnlyCategoryItemsInOrder()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "List Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Category",
            1);
        var otherCategory = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Other Category",
            2);
        var second = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Second",
            20m,
            "EUR",
            20);
        var first = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "First",
            10m,
            "EUR",
            1);
        await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            otherCategory.Id,
            "Excluded",
            1m,
            "EUR",
            0);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content.ReadFromJsonAsync<MenuItemResponse[]>();
        Assert.NotNull(content);
        Assert.Collection(
            content,
            item => Assert.Equal(first.Id.Value, item.Id),
            item => Assert.Equal(second.Id.Value, item.Id));
    }

    [Fact]
    public async Task ListMenuItemsShouldReturnEmptyArrayForEmptyCategory()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Empty Category Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Empty Category",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content.ReadFromJsonAsync<MenuItemResponse[]>();
        Assert.NotNull(content);
        Assert.Empty(content);
    }

    [Fact]
    public async Task ListMenuItemsShouldReturnNotFoundForUnknownCategory()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Missing Category Restaurant",
            DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{Guid.CreateVersion7()}/items");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.MenuItemCategoryNotFound", problem.Title);
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }

    private sealed record MenuItemResponse(
        Guid Id,
        Guid RestaurantId,
        Guid CategoryId,
        string Name,
        string? Description,
        decimal PriceAmount,
        string Currency,
        int DisplayOrder,
        bool IsAvailable,
        DateTimeOffset CreatedAtUtc,
        long Version);

    private sealed record ProblemResponse(string Title);
}
