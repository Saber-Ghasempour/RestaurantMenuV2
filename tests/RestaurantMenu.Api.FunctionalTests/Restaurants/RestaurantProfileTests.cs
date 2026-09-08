using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class RestaurantProfileTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task UpdateShouldPersistInvalidateCacheAndAppearInPublicMenu()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Profile Cafe", DateTimeOffset.UtcNow);
        var route = $"/api/restaurants/{restaurant.Id.Value}";
        using var client = factory.CreateClient();
        using var warm = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
        Assert.NotNull(await factory.GetCachedRestaurantAsync(restaurant.Id));

        using var update = await client.PutAsJsonAsync(route + "/profile",
            new { Description = " Coffee ", About = " Our story ", Address = " Lisbon ", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Null(await factory.GetCachedRestaurantAsync(restaurant.Id));
        var persisted = await factory.FindRestaurantAsync(restaurant.Id.Value);
        Assert.NotNull(persisted);
        Assert.Equal("Coffee", persisted.Description);
        Assert.Equal("Our story", persisted.About);
        Assert.Equal("Lisbon", persisted.Address);
        Assert.Equal(2, persisted.Version);

        using var read = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var cached = await factory.GetCachedRestaurantAsync(restaurant.Id);
        Assert.NotNull(cached);
        Assert.Equal("Coffee", cached.Description);

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        using var publicRead = await client.GetAsync($"/api/public/restaurants/{restaurant.Id.Value}/menu");
        Assert.Equal(HttpStatusCode.OK, publicRead.StatusCode);
        var menu = await publicRead.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Coffee", menu.GetProperty("description").GetString());
        Assert.Equal("Our story", menu.GetProperty("about").GetString());
        Assert.Equal("Lisbon", menu.GetProperty("address").GetString());
    }

    [Fact]
    public async Task StaleVersionShouldNotOverwriteProfile()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Version Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}/profile";
        using var first = await client.PutAsJsonAsync(route,
            new { Description = "First", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var stale = await client.PutAsJsonAsync(route,
            new { Description = "Stale", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("First", (await factory.FindRestaurantAsync(restaurant.Id.Value))!.Description);
    }

    [Fact]
    public async Task AnotherTenantShouldNotEditProfile()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Private Cafe", DateTimeOffset.UtcNow, "another-owner");
        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/profile",
            new { Description = "Unauthorized", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null((await factory.FindRestaurantAsync(restaurant.Id.Value))!.Description);
    }

    [Fact]
    public async Task InvalidProfileShouldReturnBadRequest()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Valid Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/profile",
            new { Description = new string('a', 501), ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, (await factory.FindRestaurantAsync(restaurant.Id.Value))!.Version);
    }
}
