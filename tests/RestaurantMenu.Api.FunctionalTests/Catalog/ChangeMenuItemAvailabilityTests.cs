using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class ChangeMenuItemAvailabilityTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ChangeMenuItemAvailabilityTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task ChangeAvailabilityShouldPersistAndReturnNewVersion()
    {
        var seeded = await SeedAsync("Availability Restaurant");
        using var client = _factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            GetAvailabilityUrl(seeded),
            new ChangeAvailabilityRequest(false, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content
            .ReadFromJsonAsync<ChangeAvailabilityResponse>();
        Assert.NotNull(content);
        Assert.Equal(seeded.MenuItemId, content.Id);
        Assert.False(content.IsAvailable);
        Assert.Equal(2, content.Version);

        var persisted = await _factory.FindMenuItemAsync(
            seeded.MenuItemId);
        Assert.NotNull(persisted);
        Assert.False(persisted.IsAvailable);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task ChangeAvailabilityShouldKeepVersionForNoOp()
    {
        var seeded = await SeedAsync("No-op Availability Restaurant");
        using var client = _factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            GetAvailabilityUrl(seeded),
            new ChangeAvailabilityRequest(true, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content
            .ReadFromJsonAsync<ChangeAvailabilityResponse>();
        Assert.NotNull(content);
        Assert.True(content.IsAvailable);
        Assert.Equal(1, content.Version);
    }

    [Fact]
    public async Task ChangeAvailabilityShouldReturnConflictForStaleVersion()
    {
        var seeded = await SeedAsync("Stale Availability Restaurant");
        using var client = _factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            GetAvailabilityUrl(seeded),
            new ChangeAvailabilityRequest(false, 42));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Catalog.MenuItemVersionConflict", problem.Title);
    }

    [Fact]
    public async Task ChangeAvailabilityShouldReturnNotFoundForWrongCategory()
    {
        var seeded = await SeedAsync("Scoped Availability Restaurant");
        var otherCategory = await _factory.SeedMenuCategoryAsync(
            seeded.RestaurantId,
            "Other Category",
            2);
        using var client = _factory.CreateClient();
        var wrongUrl =
            $"/api/restaurants/{seeded.RestaurantId}/categories/{otherCategory.Id.Value}/items/{seeded.MenuItemId}/availability";

        using var response = await client.PatchAsJsonAsync(
            wrongUrl,
            new ChangeAvailabilityRequest(false, 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var persisted = await _factory.FindMenuItemAsync(
            seeded.MenuItemId);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsAvailable);
        Assert.Equal(1, persisted.Version);
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

    private static string GetAvailabilityUrl(SeededItem seeded) =>
        $"/api/restaurants/{seeded.RestaurantId}/categories/{seeded.CategoryId}/items/{seeded.MenuItemId}/availability";

    private sealed record SeededItem(
        Guid RestaurantId,
        Guid CategoryId,
        Guid MenuItemId);

    private sealed record ChangeAvailabilityRequest(
        bool IsAvailable,
        long ExpectedVersion);

    private sealed record ChangeAvailabilityResponse(
        Guid Id,
        bool IsAvailable,
        long Version);

    private sealed record ProblemResponse(string Title);
}
