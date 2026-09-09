using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class RestaurantDefaultsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task UpdateShouldPersistRefreshManagementReadAndFlowToPublicMenu()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            "Defaults Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}";

        using var update = await client.PutAsJsonAsync(route + "/defaults", new
        {
            DefaultCurrency = "eur",
            DefaultLocale = "pt-PT",
            TimeZoneId = "Europe/Lisbon",
            ExpectedVersion = 1
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var persisted = await factory.FindRestaurantAsync(restaurant.Id.Value);
        Assert.NotNull(persisted);
        Assert.Equal("EUR", persisted.DefaultCurrency);
        Assert.Equal("pt-PT", persisted.DefaultLocale);
        Assert.Equal("Europe/Lisbon", persisted.TimeZoneId);

        using var read = await client.GetAsync(route);
        var detail = await read.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EUR", detail.GetProperty("defaultCurrency").GetString());

        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.IdentityHeader);
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        using var publicRead = await client.GetAsync(
            $"/api/public/restaurants/{restaurant.Id.Value}/menu");
        var menu = await publicRead.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pt-PT", menu.GetProperty("defaultLocale").GetString());
        Assert.Equal("Europe/Lisbon", menu.GetProperty("timeZoneId").GetString());
    }

    [Fact]
    public async Task InvalidOrForeignTenantUpdateShouldBeRejected()
    {
        await factory.MigrateDatabaseAsync();
        var ownRestaurant = await factory.SeedRestaurantAsync("Own", DateTimeOffset.UtcNow);
        var foreignRestaurant = await factory.SeedRestaurantAsync(
            "Foreign", DateTimeOffset.UtcNow, "different-owner");
        using var client = factory.CreateClient();

        using var invalid = await client.PutAsJsonAsync(
            $"/api/restaurants/{ownRestaurant.Id.Value}/defaults",
            new { DefaultCurrency = "ZZZ", DefaultLocale = "en-US", TimeZoneId = "Etc/UTC", ExpectedVersion = 1 });
        using var forbidden = await client.PutAsJsonAsync(
            $"/api/restaurants/{foreignRestaurant.Id.Value}/defaults",
            new { DefaultCurrency = "EUR", DefaultLocale = "pt-PT", TimeZoneId = "Europe/Lisbon", ExpectedVersion = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}
