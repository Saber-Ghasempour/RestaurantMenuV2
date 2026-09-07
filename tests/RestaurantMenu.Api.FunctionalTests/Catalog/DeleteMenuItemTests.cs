using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class DeleteMenuItemTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DeleteMenuItemTests(TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task DeleteMenuItemShouldSoftDeleteAndHideItem()
    {
        var seeded = await SeedAsync("Delete Item Restaurant");
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"{GetItemUrl(seeded)}?expectedVersion=1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(
            await _factory.FindMenuItemAsync(seeded.MenuItemId));
        var persisted =
            await _factory.FindMenuItemIncludingDeletedAsync(
                seeded.MenuItemId);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsDeleted);
        Assert.NotNull(persisted.DeletedAtUtc);
        Assert.Equal(2, persisted.Version);

        using var getResponse = await client.GetAsync(
            GetItemUrl(seeded));
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMenuItemShouldRemoveItemFromCategoryList()
    {
        var seeded = await SeedAsync("List After Delete Restaurant");
        using var client = _factory.CreateClient();
        using var deleteResponse = await client.DeleteAsync(
            $"{GetItemUrl(seeded)}?expectedVersion=1");
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        using var listResponse = await client.GetAsync(
            $"/api/restaurants/{seeded.RestaurantId}/categories/{seeded.CategoryId}/items");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var items = await listResponse.Content
            .ReadFromJsonAsync<List<MenuItemResponse>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task DeleteMenuItemShouldReturnConflictForStaleVersion()
    {
        var seeded = await SeedAsync("Stale Delete Item Restaurant");
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"{GetItemUrl(seeded)}?expectedVersion=42");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.MenuItemVersionConflict", problem.Title);
        Assert.NotNull(
            await _factory.FindMenuItemAsync(seeded.MenuItemId));
    }

    [Fact]
    public async Task DeleteMenuItemShouldReturnNotFoundForWrongCategory()
    {
        var seeded = await SeedAsync("Scoped Delete Item Restaurant");
        var otherCategory = await _factory.SeedMenuCategoryAsync(
            seeded.RestaurantId,
            "Other Category",
            2);
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/restaurants/{seeded.RestaurantId}/categories/{otherCategory.Id.Value}/items/{seeded.MenuItemId}?expectedVersion=1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(
            await _factory.FindMenuItemAsync(seeded.MenuItemId));
    }

    private async Task<SeededItem> SeedAsync(string restaurantName)
    {
        await _factory.MigrateDatabaseAsync();
        await _factory.MigrateCatalogDatabaseAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            restaurantName,
            DateTimeOffset.UtcNow);
        var category = await _factory.SeedMenuCategoryAsync(
            restaurant.Id.Value,
            "Category",
            1);
        var menuItem = await _factory.SeedMenuItemAsync(
            restaurant.Id.Value,
            category.Id,
            "Item",
            10m,
            "EUR",
            1);

        return new SeededItem(
            restaurant.Id.Value,
            category.Id.Value,
            menuItem.Id.Value);
    }

    private static string GetItemUrl(SeededItem seeded) =>
        $"/api/restaurants/{seeded.RestaurantId}/categories/{seeded.CategoryId}/items/{seeded.MenuItemId}";

    private sealed record SeededItem(
        Guid RestaurantId,
        Guid CategoryId,
        Guid MenuItemId);

    private sealed record MenuItemResponse(Guid Id);

    private sealed record ProblemResponse(string Title);
}
