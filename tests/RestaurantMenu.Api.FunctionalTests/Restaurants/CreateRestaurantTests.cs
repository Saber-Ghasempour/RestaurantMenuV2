using System.Net;
using System.Net.Http.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class CreateRestaurantTests
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CreateRestaurantTests(
        TestWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
    }

    [Fact]
    public async Task CreateRestaurantShouldReturnCreated()
    {
        await _factory.MigrateDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new CreateRestaurantRequest(
            "  Functional Test Restaurant  ");

        using var response =
            await client.PostAsJsonAsync(
                "/api/restaurants",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var content =
            await response.Content
                .ReadFromJsonAsync<CreateRestaurantResponse>();

        Assert.NotNull(content);
        Assert.NotEqual(Guid.Empty, content.Id);

        Assert.Equal(
            $"/api/restaurants/{content.Id}",
            response.Headers.Location?.OriginalString);

        var persistedRestaurant =
            await _factory.FindRestaurantAsync(content.Id);

        Assert.NotNull(persistedRestaurant);

        Assert.Equal(
            content.Id,
            persistedRestaurant.Id.Value);

        Assert.Equal(
            "Functional Test Restaurant",
            persistedRestaurant.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateRestaurantWithMissingNameShouldReturnBadRequest(
        string? name)
    {
        await _factory.MigrateDatabaseAsync();

        var restaurantCountBeforeRequest =
            await _factory.CountRestaurantsAsync();

        using var client = _factory.CreateClient();

        var request = new CreateRestaurantRequest(name);

        using var response =
            await client.PostAsJsonAsync(
                "/api/restaurants",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemResponse>();

        Assert.NotNull(problem);

        Assert.True(
            problem.Errors.TryGetValue(
                "Restaurants.NameRequired",
                out var validationErrors));

        Assert.Contains(
            "The restaurant name is required.",
            validationErrors);

        var restaurantCountAfterRequest =
            await _factory.CountRestaurantsAsync();

        Assert.Equal(
            restaurantCountBeforeRequest,
            restaurantCountAfterRequest);
    }

    private sealed record CreateRestaurantRequest(
        string? Name);

    private sealed record CreateRestaurantResponse(
        Guid Id);

    private sealed record ValidationProblemResponse(
        Dictionary<string, string[]> Errors);
}