using System.Net;
using System.Net.Http.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class CatalogMetadataTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly string[] MetadataTags = [" Vegan ", "quick", "VEGAN"];

    [Fact]
    public async Task FocusedCommandsShouldPersistAndExposeOnlyPublishedContent()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Metadata Cafe", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(restaurant.Id.Value, "Soups", 1);
        var item = await factory.SeedMenuItemAsync(
            restaurant.Id.Value, category.Id, "Tomato soup", 8m, "EUR", 1);
        using var client = factory.CreateClient();
        var categoryRoute = $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}";
        var itemRoute = $"{categoryRoute}/items/{item.Id.Value}";

        using var categoryContent = await client.PutAsJsonAsync(categoryRoute + "/content",
            new { Description = " Seasonal soups ", ExpectedVersion = 1 });
        using var categoryPublication = await client.PatchAsJsonAsync(categoryRoute + "/publication",
            new { IsPublished = true, ExpectedVersion = 2 });
        using var itemMetadata = await client.PutAsJsonAsync(itemRoute + "/metadata", new
        {
            Recipe = " Tomatoes and basil ",
            Calories = (int?)0,
            Tags = MetadataTags,
            AllergenNotes = " May contain nuts ",
            PreparationTimeMinutes = (int?)15,
            IsFeatured = true,
            ExpectedVersion = 1
        });
        using var unavailable = await client.PatchAsJsonAsync(itemRoute + "/availability",
            new { IsAvailable = false, ExpectedVersion = 2 });
        using var itemPublication = await client.PatchAsJsonAsync(itemRoute + "/publication",
            new { IsPublished = true, ExpectedVersion = 3 });

        Assert.Equal(HttpStatusCode.OK, categoryContent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categoryPublication.StatusCode);
        Assert.Equal(HttpStatusCode.OK, itemMetadata.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unavailable.StatusCode);
        Assert.Equal(HttpStatusCode.OK, itemPublication.StatusCode);

        var categoryRead = await client.GetFromJsonAsync<MenuCategoryResponse>(categoryRoute);
        Assert.NotNull(categoryRead);
        Assert.Equal("Seasonal soups", categoryRead.Description);
        Assert.True(categoryRead.IsPublished);
        var itemRead = await client.GetFromJsonAsync<MenuItemResponse>(itemRoute);
        Assert.NotNull(itemRead);
        Assert.Equal("Tomatoes and basil", itemRead.Recipe);
        Assert.Equal(["quick", "vegan"], itemRead.Tags);
        Assert.True(itemRead.IsFeatured);
        Assert.True(itemRead.IsPublished);

        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.IdentityHeader);
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        var publicMenu = await client.GetFromJsonAsync<PublicMenuResponse>(
            $"/api/public/restaurants/{restaurant.Id.Value}/menu");
        Assert.NotNull(publicMenu);
        var publicCategory = Assert.Single(publicMenu.Categories);
        Assert.Equal("Seasonal soups", publicCategory.Description);
        var publicItem = Assert.Single(publicCategory.Items);
        Assert.False(publicItem.IsAvailable);
        Assert.Equal(0, publicItem.Calories);
        Assert.Equal(["quick", "vegan"], publicItem.Tags);
    }

    [Fact]
    public async Task DraftCategoryAndItemShouldStayOutOfPublicMenu()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Draft Cafe", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(restaurant.Id.Value, "Draft", 1);
        await factory.SeedMenuItemAsync(restaurant.Id.Value, category.Id, "Draft item", 1m, "USD", 1);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);

        var menu = await client.GetFromJsonAsync<PublicMenuResponse>(
            $"/api/public/restaurants/{restaurant.Id.Value}/menu");

        Assert.NotNull(menu);
        Assert.Empty(menu.Categories);
    }
}
