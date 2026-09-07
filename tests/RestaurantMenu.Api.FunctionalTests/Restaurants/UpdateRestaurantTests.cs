using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class UpdateRestaurantTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UpdateRestaurantTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task UpdateRestaurantShouldReturnNewVersionAndPersistName()
    {
        await _factory.MigrateDatabaseAsync();
        var restaurant =
            await _factory.SeedRestaurantAsync(
                "Original Restaurant",
                DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();

        using var warmResponse = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}");
        Assert.Equal(HttpStatusCode.OK, warmResponse.StatusCode);
        Assert.NotNull(
            await _factory.GetCachedRestaurantAsync(restaurant.Id));

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}",
            new UpdateRestaurantRequest(
                " Updated Restaurant ",
                restaurant.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content =
            await response.Content
                .ReadFromJsonAsync<UpdateRestaurantResponse>();
        Assert.NotNull(content);
        Assert.Equal(restaurant.Id.Value, content.Id);
        Assert.Equal(2, content.Version);
        Assert.Null(
            await _factory.GetCachedRestaurantAsync(restaurant.Id));

        using var refreshedResponse = await client.GetAsync(
            $"/api/restaurants/{restaurant.Id.Value}");
        Assert.Equal(HttpStatusCode.OK, refreshedResponse.StatusCode);
        var refreshed =
            await _factory.GetCachedRestaurantAsync(restaurant.Id);
        Assert.NotNull(refreshed);
        Assert.Equal("Updated Restaurant", refreshed.Name);
        Assert.Equal(2, refreshed.Version);

        var updated =
            await _factory.FindRestaurantAsync(
                restaurant.Id.Value);
        Assert.NotNull(updated);
        Assert.Equal("Updated Restaurant", updated.Name);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task UpdateRestaurantShouldReturnBadRequestForInvalidName()
    {
        await _factory.MigrateDatabaseAsync();
        var restaurant =
            await _factory.SeedRestaurantAsync(
                "Original Restaurant",
                DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}",
            new UpdateRestaurantRequest(" ", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRestaurantShouldReturnForbiddenWithoutMembership()
    {
        await _factory.MigrateDatabaseAsync();
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{Guid.CreateVersion7()}",
            new UpdateRestaurantRequest("Updated Restaurant", 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRestaurantShouldReturnConflictForStaleVersion()
    {
        await _factory.MigrateDatabaseAsync();
        var restaurant =
            await _factory.SeedRestaurantAsync(
                "Original Restaurant",
                DateTimeOffset.UtcNow);
        using var client = _factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}",
            new UpdateRestaurantRequest(
                "Updated Restaurant",
                42));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Restaurants.VersionConflict", problem.Title);
    }

    private sealed record UpdateRestaurantRequest(
        string? Name,
        long ExpectedVersion);

    private sealed record UpdateRestaurantResponse(
        Guid Id,
        long Version);

    private sealed record ProblemResponse(string Title);
}
