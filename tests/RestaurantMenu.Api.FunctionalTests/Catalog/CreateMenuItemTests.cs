using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class CreateMenuItemTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CreateMenuItemTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task CreateMenuItemShouldReturnCreatedAndPersistMoney()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Menu Item Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Main Courses",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items",
            new CreateMenuItemRequest(
                " Carbonara ",
                " Classic pasta ",
                14.50m,
                "eur",
                10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content =
            await response.Content
                .ReadFromJsonAsync<CreateMenuItemResponse>();
        Assert.NotNull(content);
        Assert.NotEqual(Guid.Empty, content.Id);
        Assert.Equal(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{content.Id}",
            response.Headers.Location?.OriginalString);

        var persisted = await _factory.FindMenuItemAsync(content.Id);
        Assert.NotNull(persisted);
        Assert.Equal(restaurant.Id.Value, persisted.RestaurantId);
        Assert.Equal(category.Id, persisted.CategoryId);
        Assert.Equal("Carbonara", persisted.Name);
        Assert.Equal("Classic pasta", persisted.Description);
        Assert.Equal(14.50m, persisted.Price.Amount);
        Assert.Equal("EUR", persisted.Price.Currency);
        Assert.True(persisted.IsAvailable);
    }

    [Fact]
    public async Task CreateMenuItemShouldReturnNotFoundForUnknownRestaurant()
    {
        await MigrateDatabasesAsync();
        var restaurantId = Guid.CreateVersion7();
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurantId}/categories/{Guid.CreateVersion7()}/items",
            ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.RestaurantNotFound", problem.Title);
    }

    [Fact]
    public async Task CreateMenuItemShouldRejectCategoryFromAnotherRestaurant()
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
            "Owner Category",
            1);
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{otherRestaurant.Id.Value}/categories/{category.Id.Value}/items",
            ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem =
            await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.MenuItemCategoryNotFound", problem.Title);
    }

    [Theory]
    [InlineData(-1, "EUR", "Catalog.MenuItemNegativePrice")]
    [InlineData(1.999, "EUR", "Catalog.MenuItemPricePrecisionExceeded")]
    [InlineData(10, "EURO", "Catalog.MenuItemInvalidCurrency")]
    public async Task CreateMenuItemShouldRejectInvalidMoney(
        double priceAmount,
        string currency,
        string expectedErrorCode)
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Invalid Money Restaurant",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Category",
            1);
        var countBefore = await _factory.CountMenuItemsAsync();
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items",
            new CreateMenuItemRequest(
                "Invalid Item",
                null,
                (decimal)priceAmount,
                currency,
                1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(expectedErrorCode));
        Assert.Equal(countBefore, await _factory.CountMenuItemsAsync());
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }

    private static CreateMenuItemRequest ValidRequest() =>
        new("Menu Item", null, 10m, "EUR", 1);

    private sealed record CreateMenuItemRequest(
        string? Name,
        string? Description,
        decimal PriceAmount,
        string? Currency,
        int DisplayOrder);

    private sealed record CreateMenuItemResponse(Guid Id);

    private sealed record ProblemResponse(string Title);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors);
}
