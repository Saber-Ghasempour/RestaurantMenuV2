using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;

namespace RestaurantMenu.Api.FunctionalTests.Catalog;

public sealed class BranchCategoryPublicationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task AnonymousCodeMenuShouldReturnOnlyPublishedCategoriesAndInvalidateCache()
    {
        await MigrateAsync();
        var restaurant = await factory.SeedRestaurantAsync(
            "Published Bistro", DateTimeOffset.UtcNow);
        var visible = await factory.SeedMenuCategoryAsync(
            restaurant.Id.Value, "Visible", 2);
        var hidden = await factory.SeedMenuCategoryAsync(
            restaurant.Id.Value, "Hidden", 1);
        await factory.SeedMenuItemAsync(
            restaurant.Id.Value, visible.Id, "Visible item", 10m, "EUR", 1);
        await factory.SeedMenuItemAsync(
            restaurant.Id.Value, hidden.Id, "Hidden item", 12m, "EUR", 1);
        using var client = factory.CreateClient();
        var branchId = await CreateBranchAsync(client, restaurant.Id.Value, "Downtown");
        var code = await CreateCodeAsync(client, restaurant.Id.Value, branchId);
        var publicationsRoute =
            $"/api/restaurants/{restaurant.Id.Value}/branches/{branchId}/category-publications";

        using var set = await client.PutAsJsonAsync(publicationsRoute, new
        {
            Publications = new[]
            {
                new { CategoryId = visible.Id.Value, IsPublished = true,
                    DisplayOrderOverride = (int?)5 },
                new { CategoryId = hidden.Id.Value, IsPublished = false,
                    DisplayOrderOverride = (int?)null }
            }
        });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);

        using var idempotent = await client.PutAsJsonAsync(publicationsRoute, new
        {
            Publications = new[]
            {
                new { CategoryId = visible.Id.Value, IsPublished = true,
                    DisplayOrderOverride = (int?)5 },
                new { CategoryId = hidden.Id.Value, IsPublished = false,
                    DisplayOrderOverride = (int?)null }
            }
        });
        var idempotentBody = await idempotent.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, idempotent.StatusCode);
        Assert.False(idempotentBody.GetProperty("changed").GetBoolean());

        using var configuration = await client.GetAsync(publicationsRoute);
        Assert.Equal(HttpStatusCode.OK, configuration.StatusCode);
        var configurationBody = await configuration.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, configurationBody.GetArrayLength());

        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.IdentityHeader,
            TestAuthenticationHandler.AnonymousIdentity);
        using var firstMenu = await anonymous.GetAsync(
            $"/api/public/menu-codes/{code}/menu");
        Assert.Equal(HttpStatusCode.OK, firstMenu.StatusCode);
        var firstBody = await firstMenu.Content.ReadFromJsonAsync<JsonElement>();
        var firstCategory = Assert.Single(
            firstBody.GetProperty("categories").EnumerateArray().ToArray());
        Assert.Equal(visible.Id.Value, firstCategory.GetProperty("id").GetGuid());

        using var replace = await client.PutAsJsonAsync(publicationsRoute, new
        {
            Publications = new[]
            {
                new { CategoryId = visible.Id.Value, IsPublished = false,
                    DisplayOrderOverride = (int?)null },
                new { CategoryId = hidden.Id.Value, IsPublished = true,
                    DisplayOrderOverride = (int?)1 }
            }
        });
        Assert.Equal(HttpStatusCode.OK, replace.StatusCode);

        using var secondMenu = await anonymous.GetAsync(
            $"/api/public/menu-codes/{code}/menu");
        var secondBody = await secondMenu.Content.ReadFromJsonAsync<JsonElement>();
        var secondCategory = Assert.Single(
            secondBody.GetProperty("categories").EnumerateArray().ToArray());
        Assert.Equal(hidden.Id.Value, secondCategory.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ManagementShouldRejectBranchAndCategoryFromAnotherTenant()
    {
        await MigrateAsync();
        var first = await factory.SeedRestaurantAsync("First", DateTimeOffset.UtcNow);
        var second = await factory.SeedRestaurantAsync("Second", DateTimeOffset.UtcNow);
        var firstCategory = await factory.SeedMenuCategoryAsync(
            first.Id.Value, "First category", 1);
        var secondCategory = await factory.SeedMenuCategoryAsync(
            second.Id.Value, "Second category", 1);
        using var client = factory.CreateClient();
        var firstBranch = await CreateBranchAsync(client, first.Id.Value, "First branch");
        var secondBranch = await CreateBranchAsync(client, second.Id.Value, "Second branch");

        using var wrongBranch = await client.PutAsJsonAsync(
            $"/api/restaurants/{first.Id.Value}/branches/{secondBranch}/category-publications",
            new
            {
                Publications = new[]
                {
                    new { CategoryId = firstCategory.Id.Value,
                        IsPublished = true, DisplayOrderOverride = (int?)null }
                }
            });
        using var wrongCategory = await client.PutAsJsonAsync(
            $"/api/restaurants/{first.Id.Value}/branches/{firstBranch}/category-publications",
            new
            {
                Publications = new[]
                {
                    new { CategoryId = secondCategory.Id.Value,
                        IsPublished = true, DisplayOrderOverride = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.NotFound, wrongBranch.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongCategory.StatusCode);
    }

    private async Task MigrateAsync()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
    }

    private static async Task<Guid> CreateBranchAsync(
        HttpClient client, Guid restaurantId, string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurantId}/branches", new { Name = name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private static async Task<string> CreateCodeAsync(
        HttpClient client, Guid restaurantId, Guid branchId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurantId}/public-menu-codes",
            new
            {
                BranchId = branchId,
                DiningTableId = (Guid?)null,
                Purpose = 1,
                ExpiresAtUtc = (DateTimeOffset?)null
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("code").GetString()!;
    }
}
