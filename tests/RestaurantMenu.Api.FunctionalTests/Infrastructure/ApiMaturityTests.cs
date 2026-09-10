using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantMenu.Api.FunctionalTests.Infrastructure;

public sealed class ApiMaturityTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task VersionedResourceUsesEtagAndIfMatch()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            "Conditional Restaurant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();

        using var get = await client.GetAsync($"/api/restaurants/{restaurant.Id.Value}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("\"1\"", get.Headers.ETag?.Tag);

        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"/api/restaurants/{restaurant.Id.Value}")
        {
            Content = JsonContent.Create(new { name = "Updated", expectedVersion = 1 })
        };
        request.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        using var update = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("\"2\"", update.Headers.ETag?.Tag);
    }

    [Fact]
    public async Task StaleIfMatchUsesPreconditionFailedInsteadOfConflict()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            "Conditional Restaurant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"/api/restaurants/{restaurant.Id.Value}")
        {
            Content = JsonContent.Create(new { name = "Updated", expectedVersion = 42 })
        };
        request.Headers.TryAddWithoutValidation("If-Match", "\"42\"");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiContractHasStableVersionedDocument()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal("1.0", document.RootElement.GetProperty("info").GetProperty("version").GetString());
    }

}
