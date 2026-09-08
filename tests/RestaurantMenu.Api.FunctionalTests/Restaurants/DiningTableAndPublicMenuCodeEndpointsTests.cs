using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Api.Observability;

namespace RestaurantMenu.Api.FunctionalTests.Restaurants;

public sealed class DiningTableAndPublicMenuCodeEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task LifecycleShouldReturnRawCodeOnceAndResolveUntilRevoked()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("QR Restaurant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var branchesRoute = $"/api/restaurants/{restaurant.Id.Value}/branches";
        using var branchResponse = await client.PostAsJsonAsync(branchesRoute, new { Name = "Dining Room" });
        var branch = await branchResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = branch.GetProperty("id").GetGuid();
        var tablesRoute = $"{branchesRoute}/{branchId}/tables";
        using var tableResponse = await client.PostAsJsonAsync(tablesRoute,
            new { Number = 1, DisplayName = " Window ", Capacity = 4 });
        Assert.Equal(HttpStatusCode.Created, tableResponse.StatusCode);
        var table = await tableResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tableId = table.GetProperty("id").GetGuid();

        var codesRoute = $"/api/restaurants/{restaurant.Id.Value}/public-menu-codes";
        using var create = await client.PostAsJsonAsync(codesRoute, new
        {
            BranchId = branchId, DiningTableId = tableId, Purpose = 2,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30)
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var issued = await create.Content.ReadFromJsonAsync<JsonElement>();
        var codeId = issued.GetProperty("id").GetGuid();
        var rawCode = issued.GetProperty("code").GetString();
        Assert.False(string.IsNullOrWhiteSpace(rawCode));

        using var list = await client.GetAsync(codesRoute);
        var listText = await list.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.DoesNotContain(rawCode!, listText, StringComparison.Ordinal);
        Assert.DoesNotContain("codeHash", listText, StringComparison.OrdinalIgnoreCase);

        using var resolved = await client.GetAsync($"/m/{rawCode}");
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        var target = await resolved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(tableId, target.GetProperty("diningTableId").GetGuid());

        using var neverValid = await client.GetAsync($"/m/{new string('A', 43)}");
        Assert.Equal(HttpStatusCode.NotFound, neverValid.StatusCode);

        using var rotated = await client.PostAsJsonAsync($"{codesRoute}/{codeId}/rotate",
            new { ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(60), ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var rotatedBody = await rotated.Content.ReadFromJsonAsync<JsonElement>();
        var rotatedRawCode = rotatedBody.GetProperty("code").GetString();
        Assert.NotEqual(rawCode, rotatedRawCode);
        using var oldCode = await client.GetAsync($"/m/{rawCode}");
        Assert.Equal(HttpStatusCode.NotFound, oldCode.StatusCode);
        using var rotatedCode = await client.GetAsync($"/m/{rotatedRawCode}");
        Assert.Equal(HttpStatusCode.OK, rotatedCode.StatusCode);
        using var rotatedList = await client.GetAsync(codesRoute);
        var rotatedListText = await rotatedList.Content.ReadAsStringAsync();
        Assert.DoesNotContain(rotatedRawCode!, rotatedListText, StringComparison.Ordinal);
        Assert.DoesNotContain("codeHash", rotatedListText, StringComparison.OrdinalIgnoreCase);

        using var revoked = await client.PostAsJsonAsync($"{codesRoute}/{codeId}/revoke", new { ExpectedVersion = 2 });
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        using var invalid = await client.GetAsync($"/m/{rotatedRawCode}");
        var problem = await invalid.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
        Assert.DoesNotContain(rotatedRawCode!, problem, StringComparison.Ordinal);
        Assert.Contains("traceId", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void AnonymousResolverShouldCarryNamedRateLimitMetadata()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        var endpoint = Assert.Single(endpoints.OfType<RouteEndpoint>(), item => item.RoutePattern.RawText == "/m/{code}");
        var metadata = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        Assert.NotNull(metadata);
        Assert.Equal("public-menu-code-resolution", metadata.PolicyName);
        Assert.Equal("/m/{code}", CorrelationIdMiddleware.GetSafeLogPath("/m/sensitive-raw-code"));
        Assert.Equal(
            "/api/public/menu-codes/{code}/menu",
            CorrelationIdMiddleware.GetSafeLogPath(
                "/api/public/menu-codes/sensitive-raw-code/menu"));
    }

    [Fact]
    public async Task CrossTenantTableAndCodeIdentifiersShouldNotLeak()
    {
        await factory.MigrateDatabaseAsync();
        var first = await factory.SeedRestaurantAsync("First QR Tenant", DateTimeOffset.UtcNow);
        var second = await factory.SeedRestaurantAsync("Second QR Tenant", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        using var branchResponse = await client.PostAsJsonAsync($"/api/restaurants/{first.Id.Value}/branches", new { Name = "First" });
        var branch = await branchResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = branch.GetProperty("id").GetGuid();
        using var response = await client.PostAsJsonAsync($"/api/restaurants/{second.Id.Value}/public-menu-codes", new
        { BranchId = branchId, DiningTableId = (Guid?)null, Purpose = 1, ExpiresAtUtc = (DateTimeOffset?)null });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ManagementWritesShouldRequireDedicatedPermissions()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Permission QR Tenant", DateTimeOffset.UtcNow);
        using var setupClient = factory.CreateClient();
        using var branchResponse = await setupClient.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches", new { Name = "Branch" });
        var branch = await branchResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = branch.GetProperty("id").GetGuid();

        using var deniedClient = factory.CreateClient();
        deniedClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.PermissionsHeader, "branches.write");
        using var tableResponse = await deniedClient.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches/{branchId}/tables",
            new { Number = 1 });
        using var codeResponse = await deniedClient.PostAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/public-menu-codes",
            new { BranchId = (Guid?)null, DiningTableId = (Guid?)null, Purpose = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, tableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, codeResponse.StatusCode);
    }
}
