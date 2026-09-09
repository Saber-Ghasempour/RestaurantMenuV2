using System.Net;
using System.Net.Http.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Catalog.Application.Variants;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class MenuItemVariantTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task VariantLifecycleShouldPreserveOneDefaultAndOrdering()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            "Variant Bistro", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(
            restaurant.Id.Value, "Pizza", 1);
        var item = await factory.SeedMenuItemAsync(
            restaurant.Id.Value, category.Id, "Margherita", 10m, "EUR", 1);
        var route = $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{item.Id.Value}/variants";
        using var client = factory.CreateClient();

        using var create = await client.PostAsJsonAsync(route, new
        {
            Name = " Large ",
            Description = " 40 cm ",
            PriceAmount = 16.50m,
            Currency = "eur",
            DisplayOrder = 2,
            IsDefault = false
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(created);

        using var update = await client.PutAsJsonAsync($"{route}/{created.Id}", new
        {
            Name = "Family",
            Description = "50 cm",
            PriceAmount = 20m,
            Currency = "EUR",
            DisplayOrder = 1,
            ExpectedVersion = 1L
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        using var unavailable = await client.PatchAsJsonAsync(
            $"{route}/{created.Id}/availability",
            new { IsAvailable = false, ExpectedVersion = 2L });
        Assert.Equal(HttpStatusCode.OK, unavailable.StatusCode);

        using var makeDefault = await client.PatchAsJsonAsync(
            $"{route}/{created.Id}/default", new { ExpectedVersion = 3L });
        Assert.Equal(HttpStatusCode.OK, makeDefault.StatusCode);

        var variants = await client.GetFromJsonAsync<MenuItemVariantResponse[]>(route);
        Assert.NotNull(variants);
        Assert.Equal(2, variants.Length);
        Assert.Equal(created.Id, variants[0].Id);
        Assert.True(variants[0].IsDefault);
        Assert.False(variants[0].IsAvailable);
        Assert.False(variants[1].IsDefault);

        using var delete = await client.DeleteAsync(
            $"{route}/{variants[1].Id}?expectedVersion={variants[1].Version}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Single(await client.GetFromJsonAsync<MenuItemVariantResponse[]>(route) ?? []);
    }

    [Fact]
    public async Task VariantCommandsShouldRejectWrongCategoryAndDefaultDeletion()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            "Scoped Variants", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(
            restaurant.Id.Value, "Meals", 1);
        var wrongCategory = await factory.SeedMenuCategoryAsync(
            restaurant.Id.Value, "Other", 2);
        var item = await factory.SeedMenuItemAsync(
            restaurant.Id.Value, category.Id, "Meal", 10m, "EUR", 1);
        var variant = await factory.FindDefaultMenuItemVariantAsync(item.Id.Value);
        Assert.NotNull(variant);
        using var client = factory.CreateClient();

        var wrongRoute = $"/api/restaurants/{restaurant.Id.Value}/categories/{wrongCategory.Id.Value}/items/{item.Id.Value}/variants";
        using var wrongScope = await client.GetAsync(wrongRoute);
        Assert.Equal(HttpStatusCode.NotFound, wrongScope.StatusCode);

        var route = $"/api/restaurants/{restaurant.Id.Value}/categories/{category.Id.Value}/items/{item.Id.Value}/variants/{variant.Id.Value}";
        using var deleteDefault = await client.DeleteAsync(
            $"{route}?expectedVersion={variant.Version}");
        Assert.Equal(HttpStatusCode.Conflict, deleteDefault.StatusCode);
    }

    private sealed record CreatedResponse(Guid Id);
}
