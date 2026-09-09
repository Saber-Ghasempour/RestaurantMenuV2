using System.Net;
using System.Net.Http.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Media.Application.Assets;

namespace RestaurantMenu.Api.FunctionalTests.Media;

public sealed class MediaAssetEndpointsTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task InitiateShouldReturnSignedUploadAndRejectTenantDuplicate()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        await factory.MigrateMediaDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Media Cafe", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}/media-assets";
        var request = new { OriginalFileName = " dish.png ", ContentType = "image/png", SizeBytes = 68, Sha256 = Hash };

        using var response = await client.PostAsJsonAsync(route, request);
        var body = await response.Content.ReadFromJsonAsync<InitiateUploadResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains("X-Amz-Signature", body.UploadUrl.Query);
        using var duplicate = await client.PostAsJsonAsync(route, request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task InvalidContentAndCrossTenantReadShouldNotLeakAsset()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateMediaDatabaseAsync();
        var owner = await factory.SeedRestaurantAsync("Owner", DateTimeOffset.UtcNow);
        var other = await factory.SeedRestaurantAsync("Other", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        using var invalid = await client.PostAsJsonAsync($"/api/restaurants/{owner.Id.Value}/media-assets",
            new { OriginalFileName = "payload.svg", ContentType = "image/svg+xml", SizeBytes = 12, Sha256 = Hash });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var created = await client.PostAsJsonAsync($"/api/restaurants/{owner.Id.Value}/media-assets",
            new { OriginalFileName = "dish.png", ContentType = "image/png", SizeBytes = 68, Sha256 = Hash });
        var body = await created.Content.ReadFromJsonAsync<InitiateUploadResponse>();
        using var hidden = await client.GetAsync($"/api/restaurants/{other.Id.Value}/media-assets/{body!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task ReadyAssetShouldAssociateAcrossOwnersAndBlockDeletion()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        await factory.MigrateMediaDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Associations", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(restaurant.Id.Value, "Food", 1);
        var item = await factory.SeedMenuItemAsync(restaurant.Id.Value, category.Id, "Dish", 10, "USD", 1);
        var asset = await factory.SeedReadyMediaAssetAsync(restaurant.Id.Value);
        using var client = factory.CreateClient();

        using var branding = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/branding",
            new { LogoMediaId = asset.Id.Value, CoverMediaId = (Guid?)null, ExpectedVersion = 1 });
        using var categoryImage = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/image",
            new { ImageMediaId = asset.Id.Value, ExpectedVersion = 1 });
        using var itemMedia = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{item.Id.Value}/media",
            new { Media = new[] { new { MediaAssetId = asset.Id.Value, DisplayOrder = 0, AltText = "Dish", IsPrimary = true } }, ExpectedVersion = 1 });

        Assert.Equal(HttpStatusCode.OK, branding.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categoryImage.StatusCode);
        Assert.Equal(HttpStatusCode.OK, itemMedia.StatusCode);
        using var publishCategory = await client.PatchAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/publication",
            new { IsPublished = true, ExpectedVersion = 2 });
        using var publishItem = await client.PatchAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{item.Id.Value}/publication",
            new { IsPublished = true, ExpectedVersion = 2 });
        Assert.Equal(HttpStatusCode.OK, publishCategory.StatusCode);
        Assert.Equal(HttpStatusCode.OK, publishItem.StatusCode);
        using var publicClient = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false });
        using var publicMedia = await publicClient.GetAsync(
            $"/api/public/restaurants/{restaurant.Id.Value}/media-assets/{asset.Id.Value}");
        Assert.Equal(HttpStatusCode.Redirect, publicMedia.StatusCode);
        Assert.Equal("localhost", publicMedia.Headers.Location?.Host);
        using var deletion = await client.DeleteAsync($"/api/restaurants/{restaurant.Id.Value}/media-assets/{asset.Id.Value}?expectedVersion=2");
        Assert.Equal(HttpStatusCode.Conflict, deletion.StatusCode);
    }
}
