using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

namespace RestaurantMenu.Api.FunctionalTests.PublicMenus;

public sealed class GetPublicMenuTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GetPublicMenuTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task PublicMenuShouldAllowAnonymousAccessAndReturnAvailableItems()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "QR Bistro",
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Drinks",
            1,
            isPublished: true);
        var availableItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Espresso",
            2.50m,
            "EUR",
            1,
            isPublished: true);
        var unavailableItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Seasonal Drink",
            4.50m,
            "EUR",
            2,
            isPublished: true);
        await _factory.SetMenuItemAvailabilityAsync(
            unavailableItem.Id,
            false);
        using var client = CreateAnonymousClient();

        using var response = await client.GetAsync(
            $"/api/public/restaurants/{restaurant.Id.Value}/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content.ReadFromJsonAsync<PublicMenuResponse>();
        Assert.NotNull(content);
        Assert.Equal(restaurant.Id.Value, content.RestaurantId);
        Assert.Equal("QR Bistro", content.RestaurantName);
        var publicCategory = Assert.Single(content.Categories);
        Assert.Equal(category.Id.Value, publicCategory.Id);
        Assert.Equal(2, publicCategory.Items.Count);
        var publicItem = publicCategory.Items[0];
        Assert.Equal(availableItem.Id.Value, publicItem.Id);
        Assert.Equal("Espresso", publicItem.Name);
        Assert.Equal(2.50m, publicItem.PriceAmount);
        Assert.Equal("EUR", publicItem.Currency);
        Assert.True(publicItem.IsAvailable);
        Assert.Equal(unavailableItem.Id.Value, publicCategory.Items[1].Id);
        Assert.False(publicCategory.Items[1].IsAvailable);
    }

    [Fact]
    public async Task PublicMenuShouldReturnNotFoundForUnknownRestaurant()
    {
        await MigrateDatabasesAsync();
        using var client = CreateAnonymousClient();

        using var response = await client.GetAsync(
            $"/api/public/restaurants/{Guid.CreateVersion7()}/menu");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicMenuShouldHideSoftDeletedCategoriesAndItems()
    {
        await MigrateDatabasesAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Filtered Bistro",
            DateTimeOffset.UtcNow);
        var visibleCategory = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Visible",
            1,
            isPublished: true);
        var deletedCategory = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Deleted",
            2,
            isPublished: true);
        var visibleItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            visibleCategory.Id,
            "Visible Item",
            5m,
            "EUR",
            1,
            isPublished: true);
        var deletedItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            visibleCategory.Id,
            "Deleted Item",
            6m,
            "EUR",
            2,
            isPublished: true);
        await _factory.DeleteMenuItemAsync(deletedItem.Id);
        await _factory.DeleteMenuCategoryAsync(deletedCategory.Id);
        using var client = CreateAnonymousClient();

        using var response = await client.GetAsync(
            $"/api/public/restaurants/{restaurant.Id.Value}/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content.ReadFromJsonAsync<PublicMenuResponse>();
        Assert.NotNull(content);
        var category = Assert.Single(content.Categories);
        Assert.Equal(visibleCategory.Id.Value, category.Id);
        var item = Assert.Single(category.Items);
        Assert.Equal(visibleItem.Id.Value, item.Id);
    }

    [Fact]
    public async Task ManagementReadShouldRemainProtectedForAnonymousCustomer()
    {
        using var client = CreateAnonymousClient();

        using var response = await client.GetAsync(
            $"/api/restaurants/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateAnonymousClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        return client;
    }

    private async Task MigrateDatabasesAsync()
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
    }
}
