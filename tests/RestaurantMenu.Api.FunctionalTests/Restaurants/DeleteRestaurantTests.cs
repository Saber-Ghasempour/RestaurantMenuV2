using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class DeleteRestaurantTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DeleteRestaurantTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task DeleteRestaurantShouldSoftDeleteAndReturnNoContent()
    {
        await _factory.MigrateDatabaseAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Restaurant To Delete",
            DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();

        using var warmResponse = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}");
        Assert.Equal(HttpStatusCode.OK, warmResponse.StatusCode);
        Assert.NotNull(
            await _factory.GetCachedRestaurantAsync(restaurant.Id));

        using var response = await client.DeleteAsync(
            $"/api/restaurants/{restaurant.Id.Value}?expectedVersion=1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(
            await _factory.GetCachedRestaurantAsync(restaurant.Id));

        using var getAfterDeleteResponse = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}");
        Assert.Equal(
            HttpStatusCode.NotFound,
            getAfterDeleteResponse.StatusCode);
        Assert.Null(
            await _factory.FindRestaurantAsync(
                restaurant.Id.Value));

        var persisted =
            await _factory.FindRestaurantIncludingDeletedAsync(
                restaurant.Id.Value);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsDeleted);
        Assert.NotNull(persisted.DeletedAtUtc);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task DeleteRestaurantShouldReturnNotFoundForUnknownId()
    {
        await _factory.MigrateDatabaseAsync();
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/restaurants/{Guid.CreateVersion7()}?expectedVersion=1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRestaurantShouldReturnConflictForStaleVersion()
    {
        await _factory.MigrateDatabaseAsync();
        var restaurant = await _factory.SeedRestaurantAsync(
            "Restaurant To Keep",
            DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/restaurants/{restaurant.Id.Value}?expectedVersion=42");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Restaurants.VersionConflict", problem.Title);
        Assert.NotNull(
            await _factory.FindRestaurantAsync(
                restaurant.Id.Value));
    }

    private sealed record ProblemResponse(string Title);
}
