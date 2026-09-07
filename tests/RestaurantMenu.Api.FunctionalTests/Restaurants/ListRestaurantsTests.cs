using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class ListRestaurantsTests
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 6, 20, 0, 0, TimeSpan.Zero);

    private readonly TestWebApplicationFactory _factory;

    public ListRestaurantsTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
    }

    [Fact]
    public async Task ListRestaurantsShouldReturnRequestedPage()
    {
        await _factory.MigrateDatabaseAsync();

        await _factory.SeedRestaurantAsync(
            "Oldest Restaurant",
            CreatedAtUtc.AddHours(-2));

        var middle =
            await _factory.SeedRestaurantAsync(
                "Middle Restaurant",
                CreatedAtUtc.AddHours(-1));

        var newest =
            await _factory.SeedRestaurantAsync(
                "Newest Restaurant",
                CreatedAtUtc);

        await _factory.SeedRestaurantAsync(
            "Another Tenant Restaurant",
            CreatedAtUtc.AddHours(1),
            "another-tenant-user");

        using var client = _factory.CreateClient();

        using var response =
            await client.GetAsync(
                "/api/restaurants?page=1&pageSize=2");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<ListRestaurantsResponse>();

        Assert.NotNull(content);
        Assert.Equal(1, content.Page);
        Assert.Equal(2, content.PageSize);
        Assert.Equal(3, content.TotalCount);
        Assert.Equal(2, content.TotalPages);

        Assert.Collection(
            content.Items,
            restaurant =>
            {
                Assert.Equal(newest.Id.Value, restaurant.Id);
                Assert.Equal(newest.Name, restaurant.Name);
                Assert.Equal(1, restaurant.Version);
            },
            restaurant =>
            {
                Assert.Equal(middle.Id.Value, restaurant.Id);
                Assert.Equal(middle.Name, restaurant.Name);
                Assert.Equal(1, restaurant.Version);
            });
    }

    [Fact]
    public async Task ListRestaurantsShouldRejectInvalidPage()
    {
        await _factory.MigrateDatabaseAsync();

        using var client = _factory.CreateClient();

        using var response =
            await client.GetAsync(
                "/api/restaurants?page=0&pageSize=20");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemResponse>();

        Assert.NotNull(problem);

        Assert.True(
            problem.Errors.ContainsKey(
                "Restaurants.InvalidPage"));
    }

    private sealed record ListRestaurantsResponse(
        IReadOnlyList<RestaurantResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private sealed record RestaurantResponse(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAtUtc,
        long Version);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors);
}
