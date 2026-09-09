using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Api.Observability;
using RestaurantMenu.Ordering.Presentation.DiningSessions;

namespace RestaurantMenu.Api.FunctionalTests.Ordering;
public sealed class DiningSessionEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task DineInCodeShouldIssueHeaderAndCookieWithoutLeakingTokenInBodyOrStorage()
    {
        await factory.MigrateDatabaseAsync(); await factory.MigrateOrderingDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Sessions", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var issuedCode = await CreateCodeAsync(client, restaurant.Id.Value, purpose: 2);

        using var response = await client.PostAsync($"/api/public/menu-codes/{issuedCode.Code}/sessions", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = Assert.Single(response.Headers.GetValues(DiningSessionEndpoints.HeaderName));
        Assert.Equal(43, token.Length);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("HttpOnly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(token, text, StringComparison.Ordinal);
        var body = JsonDocument.Parse(text).RootElement;
        var sessionId = body.GetProperty("id").GetGuid();
        var persisted = await factory.FindDiningSessionAsync(sessionId);
        Assert.NotNull(persisted); Assert.NotEqual(token, persisted.TokenHash);

        using var headerRequest = new HttpRequestMessage(HttpMethod.Get, "/api/public/dining-sessions/current");
        headerRequest.Headers.Add(DiningSessionEndpoints.HeaderName, token);
        using var current = await client.SendAsync(headerRequest);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);

        using var wrongTableRequest = new HttpRequestMessage(HttpMethod.Get,
            $"/api/public/dining-sessions/current?diningTableId={Guid.NewGuid()}");
        wrongTableRequest.Headers.Add(DiningSessionEndpoints.HeaderName, token);
        using var wrongTable = await client.SendAsync(wrongTableRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongTable.StatusCode);

        using var cookieRequest = new HttpRequestMessage(HttpMethod.Get, "/api/public/dining-sessions/current");
        cookieRequest.Headers.Add("Cookie", $"{DiningSessionEndpoints.CookieName}={token}");
        using var byCookie = await client.SendAsync(cookieRequest);
        Assert.Equal(HttpStatusCode.OK, byCookie.StatusCode);
    }

    [Fact]
    public async Task MenuOnlyInactiveAndMismatchedCredentialsShouldBeRejectedGenerically()
    {
        await factory.MigrateDatabaseAsync(); await factory.MigrateOrderingDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Session rejection", DateTimeOffset.UtcNow);
        using var client = factory.CreateClient();
        var menuOnly = await CreateCodeAsync(client, restaurant.Id.Value, purpose: 1);
        using var denied = await client.PostAsync($"/api/public/menu-codes/{menuOnly.Code}/sessions", null);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.DoesNotContain(menuOnly.Code, await denied.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var dineIn = await CreateCodeAsync(client, restaurant.Id.Value, purpose: 2);
        using var started = await client.PostAsync($"/api/public/menu-codes/{dineIn.Code}/sessions", null);
        var token = Assert.Single(started.Headers.GetValues(DiningSessionEndpoints.HeaderName));
        using var mismatch = new HttpRequestMessage(HttpMethod.Get, "/api/public/dining-sessions/current");
        mismatch.Headers.Add(DiningSessionEndpoints.HeaderName, token);
        mismatch.Headers.Add("Cookie", $"{DiningSessionEndpoints.CookieName}={new string('A', 43)}");
        using var mismatchResponse = await client.SendAsync(mismatch);
        Assert.Equal(HttpStatusCode.Unauthorized, mismatchResponse.StatusCode);

        var inactiveTable = await CreateCodeAsync(client, restaurant.Id.Value, purpose: 2);
        using var tableStatus = await client.PatchAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches/{inactiveTable.BranchId}/tables/{inactiveTable.TableId}/status",
            new { IsActive = false, ExpectedVersion = 1 });
        tableStatus.EnsureSuccessStatusCode();
        using var inactiveTableResponse = await client.PostAsync(
            $"/api/public/menu-codes/{inactiveTable.Code}/sessions", null);
        Assert.Equal(HttpStatusCode.NotFound, inactiveTableResponse.StatusCode);

        var inactiveBranch = await CreateCodeAsync(client, restaurant.Id.Value, purpose: 2);
        using var branchStatus = await client.PatchAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches/{inactiveBranch.BranchId}/status",
            new { IsActive = false, ExpectedVersion = 1 });
        branchStatus.EnsureSuccessStatusCode();
        using var inactiveBranchResponse = await client.PostAsync(
            $"/api/public/menu-codes/{inactiveBranch.Code}/sessions", null);
        Assert.Equal(HttpStatusCode.NotFound, inactiveBranchResponse.StatusCode);
    }

    [Fact]
    public void EndpointsShouldCarryRateLimitsAndSecretPathsShouldBeRedacted()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
        var start = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == "/api/public/menu-codes/{code}/sessions");
        Assert.Equal("dining-session-start", start.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.Equal("/api/public/menu-codes/{code}/sessions",
            CorrelationIdMiddleware.GetSafeLogPath("/api/public/menu-codes/raw-secret/sessions"));
    }

    private static async Task<(string Code, Guid BranchId, Guid TableId)> CreateCodeAsync(HttpClient client,
        Guid restaurantId, int purpose)
    {
        Guid? branchId = null; Guid? tableId = null;
        if (purpose == 2)
        {
            using var branchResponse = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/branches", new { Name = $"Room {Guid.NewGuid():N}" });
            var branch = await branchResponse.Content.ReadFromJsonAsync<JsonElement>(); branchId = branch.GetProperty("id").GetGuid();
            using var tableResponse = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/branches/{branchId}/tables", new { Number = 1 });
            if (!tableResponse.IsSuccessStatusCode)
                throw new InvalidOperationException(await tableResponse.Content.ReadAsStringAsync());
            var table = await tableResponse.Content.ReadFromJsonAsync<JsonElement>(); tableId = table.GetProperty("id").GetGuid();
        }
        using var codeResponse = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/public-menu-codes",
            new { BranchId = branchId, DiningTableId = tableId, Purpose = purpose });
        codeResponse.EnsureSuccessStatusCode();
        var code = await codeResponse.Content.ReadFromJsonAsync<JsonElement>();
        return (code.GetProperty("code").GetString()!, branchId ?? Guid.Empty, tableId ?? Guid.Empty);
    }
}
