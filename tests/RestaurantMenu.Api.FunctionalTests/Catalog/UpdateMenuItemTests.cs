using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class UpdateMenuItemTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UpdateMenuItemTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task UpdateMenuItemShouldPersistChangesAndReturnVersion()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Update Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Main Courses",
            1);
        var menuItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Original Item",
            10m,
            "EUR",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{menuItem.Id.Value}",
            new UpdateMenuItemRequest(
                " Updated Item ",
                " Updated description ",
                19.95m,
                "usd",
                20,
                1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content
            .ReadFromJsonAsync<UpdateMenuItemResponse>();
        Assert.NotNull(content);
        Assert.Equal(menuItem.Id.Value, content.Id);
        Assert.Equal(2, content.Version);

        var persisted = await _factory.FindMenuItemAsync(
            menuItem.Id.Value);
        Assert.NotNull(persisted);
        Assert.Equal("Updated Item", persisted.Name);
        Assert.Equal("Updated description", persisted.Description);
        var variant = await _factory.FindDefaultMenuItemVariantAsync(menuItem.Id.Value);
        Assert.NotNull(variant);
        Assert.Equal(19.95m, variant.Price.Amount);
        Assert.Equal("USD", variant.Price.Currency);
        Assert.Equal(20, persisted.DisplayOrder);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task UpdateMenuItemShouldReturnBadRequestForInvalidPrice()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Invalid Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Category",
            1);
        var menuItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Original",
            10m,
            "EUR",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{menuItem.Id.Value}",
            new UpdateMenuItemRequest(
                "Updated",
                null,
                -1m,
                "EUR",
                1,
                1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.True(
            problem.Errors.ContainsKey(
                "Catalog.MenuItemNegativePrice"));
    }

    [Fact]
    public async Task UpdateMenuItemShouldReturnConflictForStaleVersion()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Stale Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Category",
            1);
        var menuItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Original",
            10m,
            "EUR",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{menuItem.Id.Value}",
            new UpdateMenuItemRequest(
                "Updated",
                null,
                10m,
                "EUR",
                1,
                42));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.MenuItemVersionConflict", problem.Title);
    }

    [Fact]
    public async Task UpdateMenuItemShouldReturnNotFoundForWrongCategory()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Scoped Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Owner Category",
            1);
        var otherCategory = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Other Category",
            2);
        var menuItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Original",
            10m,
            "EUR",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{otherCategory.Id.Value}/items/{menuItem.Id.Value}",
            new UpdateMenuItemRequest(
                "Updated",
                null,
                10m,
                "EUR",
                1,
                1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var persisted = await _factory.FindMenuItemAsync(
            menuItem.Id.Value);
        Assert.NotNull(persisted);
        Assert.Equal("Original", persisted.Name);
        Assert.Equal(1, persisted.Version);
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }

    private sealed record UpdateMenuItemRequest(
        string? Name,
        string? Description,
        decimal PriceAmount,
        string? Currency,
        int DisplayOrder,
        long ExpectedVersion);

    private sealed record UpdateMenuItemResponse(
        Guid Id,
        long Version);

    private sealed record ProblemResponse(string Title);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors);
}
