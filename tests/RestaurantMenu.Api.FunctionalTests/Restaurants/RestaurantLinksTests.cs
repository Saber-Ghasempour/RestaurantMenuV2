using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class RestaurantLinksTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task LinksShouldPersistRefreshCacheAndAppearInAnonymousMenu()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Links Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}";
        using var warm = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
        Assert.NotNull(await factory.GetCachedRestaurantAsync(restaurant.Id));

        using var update = await client.PutAsJsonAsync(route + "/links", new
        {
            WebsiteUrl = " https://example.com ",
            InstagramUrl = "https://instagram.com/cafe",
            FacebookUrl = "https://facebook.com/cafe",
            WhatsAppUrl = "https://wa.me/12345",
            TelegramUrl = "https://t.me/cafe",
            TwitterUrl = "https://x.com/cafe",
            ExpectedVersion = 1
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Null(await factory.GetCachedRestaurantAsync(restaurant.Id));
        var persisted = await factory.FindRestaurantAsync(restaurant.Id.Value);
        Assert.NotNull(persisted);
        Assert.Equal("https://example.com", persisted.WebsiteUrl);
        Assert.Equal("https://instagram.com/cafe", persisted.InstagramUrl);
        Assert.Equal("https://facebook.com/cafe", persisted.FacebookUrl);
        Assert.Equal("https://wa.me/12345", persisted.WhatsAppUrl);
        Assert.Equal("https://t.me/cafe", persisted.TelegramUrl);
        Assert.Equal("https://x.com/cafe", persisted.TwitterUrl);
        Assert.Equal(2, persisted.Version);

        using var detail = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var cached = await factory.GetCachedRestaurantAsync(restaurant.Id);
        Assert.NotNull(cached);
        Assert.Equal(persisted.WebsiteUrl, cached.WebsiteUrl);
        Assert.Equal(persisted.TwitterUrl, cached.TwitterUrl);

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        using var publicResponse = await client.GetAsync($"/api/public/restaurants/{restaurant.Id.Value}/menu");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var menu = await publicResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(persisted.WebsiteUrl, menu.GetProperty("websiteUrl").GetString());
        Assert.Equal(persisted.InstagramUrl, menu.GetProperty("instagramUrl").GetString());
        Assert.Equal(persisted.FacebookUrl, menu.GetProperty("facebookUrl").GetString());
        Assert.Equal(persisted.WhatsAppUrl, menu.GetProperty("whatsAppUrl").GetString());
        Assert.Equal(persisted.TelegramUrl, menu.GetProperty("telegramUrl").GetString());
        Assert.Equal(persisted.TwitterUrl, menu.GetProperty("twitterUrl").GetString());
    }

    [Fact]
    public async Task InvalidLinksShouldNotModifyExistingData()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Validation Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/links",
            new { WebsiteUrl = "https://valid.example", TelegramUrl = "javascript:alert(1)", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var persisted = await factory.FindRestaurantAsync(restaurant.Id.Value);
        Assert.NotNull(persisted);
        Assert.Null(persisted.WebsiteUrl);
        Assert.Null(persisted.TelegramUrl);
        Assert.Equal(1, persisted.Version);
    }

    [Fact]
    public async Task StaleUpdateShouldFailAndExplicitReplacementShouldClearLinks()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Versions Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}/links";
        using var first = await client.PutAsJsonAsync(route,
            new { WebsiteUrl = "https://first.example", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var stale = await client.PutAsJsonAsync(route,
            new { WebsiteUrl = "https://stale.example", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("https://first.example", (await factory.FindRestaurantAsync(restaurant.Id.Value))!.WebsiteUrl);
        using var clear = await client.PutAsJsonAsync(route, new { ExpectedVersion = 2 });
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        Assert.Null((await factory.FindRestaurantAsync(restaurant.Id.Value))!.WebsiteUrl);
    }

    [Theory]
    [InlineData("anonymous", "test-user", "restaurants.write", HttpStatusCode.Unauthorized)]
    [InlineData("authenticated", "other-user", "restaurants.write", HttpStatusCode.Forbidden)]
    [InlineData("authenticated", "test-user", "restaurants.read", HttpStatusCode.Forbidden)]
    public async Task UnauthorizedCallersShouldNotChangeLinks(
        string identity, string subject, string permissions, HttpStatusCode expectedStatus)
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Access Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, identity);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.PermissionsHeader, permissions);
        using var response = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/links",
            new { WebsiteUrl = "https://unauthorized.example", ExpectedVersion = 1 });
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Null((await factory.FindRestaurantAsync(restaurant.Id.Value))!.WebsiteUrl);
    }
}
