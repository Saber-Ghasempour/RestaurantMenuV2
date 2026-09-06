using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class GetRestaurantTests
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 6, 18, 0, 0, TimeSpan.Zero);

    private readonly TestWebApplicationFactory _factory;

    public GetRestaurantTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
    }

    [Fact]
    public async Task GetRestaurantShouldReturnOkWhenRestaurantExists()
    {
        await _factory.MigrateDatabaseAsync();

        var restaurant =
            await _factory.SeedRestaurantAsync(
                "Get Restaurant Test",
                CreatedAtUtc);

        using var client = _factory.CreateClient();

        using var response =
            await client.GetAsync(
                $"/api/restaurants/{restaurant.Id.Value}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<GetRestaurantResponse>();

        Assert.NotNull(content);
        Assert.Equal(restaurant.Id.Value, content.Id);
        Assert.Equal("Get Restaurant Test", content.Name);
        Assert.Equal(CreatedAtUtc, content.CreatedAtUtc);
        Assert.Equal(1, content.Version);
    }

    [Fact]
    public async Task GetRestaurantShouldReturnNotFoundWhenRestaurantDoesNotExist()
    {
        await _factory.MigrateDatabaseAsync();

        var restaurantId = Guid.CreateVersion7();

        using var client = _factory.CreateClient();

        using var response =
            await client.GetAsync(
                $"/api/restaurants/{restaurantId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemResponse>();

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal(
            "Restaurants.NotFound",
            problem.Title);

        Assert.Contains(
            restaurantId.ToString(),
            problem.Detail);
    }

    private sealed record GetRestaurantResponse(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAtUtc,
        long Version);

    private sealed record ProblemResponse(
        int Status,
        string Title,
        string Detail);
}
