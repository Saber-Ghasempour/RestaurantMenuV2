using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class RestaurantSlugTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task SlugShouldResolvePublicMenuAndInvalidateCachedDetails()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Slug Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}";
        var slug = "cafe-" + Guid.NewGuid().ToString("N");
        using var warm = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
        Assert.NotNull(await factory.GetCachedRestaurantAsync(restaurant.Id));
        using var update = await client.PutAsJsonAsync(route + "/slug",
            new { Slug = " " + slug.ToUpperInvariant() + " ", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Null(await factory.GetCachedRestaurantAsync(restaurant.Id));
        using var refreshed = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(slug, (await factory.GetCachedRestaurantAsync(restaurant.Id))!.Slug);

        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        using var menu = await anonymous.GetAsync($"/api/public/restaurants/by-slug/{slug}/menu");
        Assert.Equal(HttpStatusCode.OK, menu.StatusCode);
        var body = await menu.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(restaurant.Id.Value, body.GetProperty("restaurantId").GetGuid());
        Assert.Equal(slug, body.GetProperty("slug").GetString());

        using var rename = await client.PutAsJsonAsync(route + "/slug",
            new { Slug = slug + "-new", ExpectedVersion = 2 });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        using var old = await anonymous.GetAsync($"/api/public/restaurants/by-slug/{slug}/menu");
        Assert.Equal(HttpStatusCode.NotFound, old.StatusCode);
        using var current = await anonymous.GetAsync($"/api/public/restaurants/by-slug/{slug}-new/menu");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
    }

    [Fact]
    public async Task ConcurrentClaimsShouldHaveExactlyOneWinner()
    {
        await factory.MigrateDatabaseAsync();
        var first = await factory.SeedRestaurantAsync("First Cafe", DateTimeOffset.UtcNow);
        var second = await factory.SeedRestaurantAsync("Second Cafe", DateTimeOffset.UtcNow);
        var slug = "unique-" + Guid.NewGuid().ToString("N");
        using var client = factory.CreateClient();
        var responses = await Task.WhenAll(
            client.PutAsJsonAsync($"/api/restaurants/{first.Id.Value}/slug",
                new { Slug = slug, ExpectedVersion = 1 }),
            client.PutAsJsonAsync($"/api/restaurants/{second.Id.Value}/slug",
                new { Slug = slug.ToUpperInvariant(), ExpectedVersion = 1 }));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            var body = await conflict.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Restaurants.SlugAlreadyExists", body.GetProperty("title").GetString());
            var persisted = new[]
            {
                await factory.FindRestaurantAsync(first.Id.Value),
                await factory.FindRestaurantAsync(second.Id.Value)
            };
            Assert.Single(persisted, restaurant => restaurant!.Slug == slug);
            Assert.Single(persisted, restaurant => restaurant!.Slug is null && restaurant.Version == 1);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task DeletedRestaurantShouldNotResolveAndShouldRetainSlugReservation()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var first = await factory.SeedRestaurantAsync("Deleted Cafe", DateTimeOffset.UtcNow);
        var second = await factory.SeedRestaurantAsync("Other Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var slug = "reserved-" + Guid.NewGuid().ToString("N");
        using var claim = await client.PutAsJsonAsync($"/api/restaurants/{first.Id.Value}/slug",
            new { Slug = slug, ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, claim.StatusCode);
        using var deletion = await client.DeleteAsync($"/api/restaurants/{first.Id.Value}?expectedVersion=2");
        Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
        using var menu = await client.GetAsync($"/api/public/restaurants/by-slug/{slug}/menu");
        Assert.Equal(HttpStatusCode.NotFound, menu.StatusCode);
        using var reuse = await client.PutAsJsonAsync($"/api/restaurants/{second.Id.Value}/slug",
            new { Slug = slug, ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
    }

    [Fact]
    public async Task StaleAndInvalidUpdatesShouldNotChangeSlug()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Validation Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}/slug";
        using var invalid = await client.PutAsJsonAsync(route, new { Slug = "bad slug", ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var stale = await client.PutAsJsonAsync(route, new { Slug = "valid-slug", ExpectedVersion = 42 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Null((await factory.FindRestaurantAsync(restaurant.Id.Value))!.Slug);
    }

    [Fact]
    public async Task WhitespacePublicSlugShouldReturnNotFoundInsteadOfServerError()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);

        using var response = await client.GetAsync(
            "/api/public/restaurants/by-slug/%20/menu");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("anonymous", "test-user", "restaurants.write", HttpStatusCode.Unauthorized)]
    [InlineData("authenticated", "other", "restaurants.write", HttpStatusCode.Forbidden)]
    [InlineData("authenticated", "test-user", "restaurants.read", HttpStatusCode.Forbidden)]
    public async Task SlugRequiresPermissionAndMembership(
        string identity, string subject, string permission, HttpStatusCode expected)
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Secure Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, identity);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.PermissionsHeader, permission);
        using var response = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/slug",
            new { Slug = "secure-slug", ExpectedVersion = 1 });
        Assert.Equal(expected, response.StatusCode);
        Assert.Null((await factory.FindRestaurantAsync(restaurant.Id.Value))!.Slug);
    }
}
