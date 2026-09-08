using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Presentation.Abstractions.Authorization;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class BranchEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task BranchLifecycleShouldPersistAndExposeManagementProjection()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Branch Restaurant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var collection = $"/api/restaurants/{restaurant.Id.Value}/branches";

        using var created = await client.PostAsJsonAsync(collection, new
        {
            Name = " Downtown ", Slug = " Main-Branch ", Phone = "+351210000000",
            AddressLine = "1 Main Street", CityName = "Lisbon", RegionName = "Lisbon",
            PostalCode = "1000-001", CountryCode = "pt", Latitude = 38.7223m,
            Longitude = -9.1393m, TimeZoneId = "Europe/Lisbon"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = createdBody.GetProperty("id").GetGuid();
        Assert.Equal($"{collection}/{branchId}", created.Headers.Location?.OriginalString);

        using var get = await client.GetAsync($"{collection}/{branchId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var detail = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Downtown", detail.GetProperty("name").GetString());
        Assert.Equal("main-branch", detail.GetProperty("slug").GetString());
        Assert.Equal("PT", detail.GetProperty("countryCode").GetString());
        Assert.True(detail.GetProperty("isActive").GetBoolean());
        Assert.Equal(1, detail.GetProperty("version").GetInt64());

        using var list = await client.GetAsync(collection + "?search=down&isActive=true");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, listBody.GetProperty("totalCount").GetInt32());

        using var updated = await client.PutAsJsonAsync($"{collection}/{branchId}", new
        {
            Name = "Riverside", Slug = "riverside", Phone = (string?)null,
            AddressLine = (string?)null, CityName = (string?)null,
            RegionName = (string?)null, PostalCode = (string?)null,
            CountryCode = (string?)null, Latitude = (decimal?)null,
            Longitude = (decimal?)null, TimeZoneId = (string?)null, ExpectedVersion = 1
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        using var status = await client.PatchAsJsonAsync($"{collection}/{branchId}/status",
            new { IsActive = false, ExpectedVersion = 2 });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        using var deleted = await client.DeleteAsync($"{collection}/{branchId}?expectedVersion=3");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Null(await factory.FindBranchAsync(branchId));
        var persisted = await factory.FindBranchIncludingDeletedAsync(branchId);
        Assert.NotNull(persisted);
        Assert.False(persisted.IsActive);
        Assert.Equal(4, persisted.Version);
    }

    [Fact]
    public async Task InvalidBranchShouldReturnValidationProblemWithoutPersisting()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Validation Restaurant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches",
            new { Name = "Branch", Slug = "bad slug", Latitude = 10m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors")
            .TryGetProperty("Branches.InvalidSlug", out var errors));
        Assert.NotEmpty(errors.EnumerateArray());
    }

    [Fact]
    public async Task BranchFromAnotherRestaurantShouldNotBeAddressableThroughRoute()
    {
        await factory.MigrateDatabaseAsync();
        var first = await factory.SeedRestaurantAsync("First Branch Tenant", DateTimeOffset.UtcNow);
        var second = await factory.SeedRestaurantAsync("Second Branch Tenant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            $"/api/restaurants/{first.Id.Value}/branches", new { Name = "First Branch" });
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = body.GetProperty("id").GetGuid();

        using var response = await client.GetAsync(
            $"/api/restaurants/{second.Id.Value}/branches/{branchId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateSlugInsideRestaurantShouldReturnConflict()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Slug Branch Tenant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var route = $"/api/restaurants/{restaurant.Id.Value}/branches";
        using var first = await client.PostAsJsonAsync(route,
            new { Name = "First", Slug = "shared-branch" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var duplicate = await client.PostAsJsonAsync(route,
            new { Name = "Second", Slug = "SHARED-BRANCH" });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Branches.SlugAlreadyExists", problem.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("anonymous", "test-user", "branches.write", HttpStatusCode.Unauthorized)]
    [InlineData("authenticated", "other-user", "branches.write", HttpStatusCode.Forbidden)]
    [InlineData("authenticated", "test-user", "branches.read", HttpStatusCode.Forbidden)]
    public async Task CreateShouldRequirePermissionAndRestaurantMembership(
        string identity,
        string subject,
        string permission,
        HttpStatusCode expected)
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Secure Branch Tenant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, identity);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.PermissionsHeader, permission);

        using var response = await client.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches", new { Name = "Denied" });

        Assert.Equal(expected, response.StatusCode);
    }
}
