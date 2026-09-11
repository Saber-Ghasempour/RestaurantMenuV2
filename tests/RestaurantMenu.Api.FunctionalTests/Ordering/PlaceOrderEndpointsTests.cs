using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Ordering.Presentation.DiningSessions;
using RestaurantMenu.Ordering.Presentation.Orders;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Api.FunctionalTests.Ordering;

public sealed class PlaceOrderEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task PlaceOrderShouldTrustSessionAndCatalogAndReplayIdenticalKey()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateCatalogDatabaseAsync();
        await factory.MigrateOrderingDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Ordering", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(restaurant.Id.Value, "Meals", 0, isPublished: true);
        var item = await factory.SeedMenuItemAsync(restaurant.Id.Value, category.Id, "Server burger", 8.75m, "EUR", 0, isPublished: true);
        var variant = await factory.FindDefaultMenuItemVariantAsync(item.Id.Value);
        Assert.NotNull(variant);
        using var client = factory.CreateClient();
        var branchId = await CreateBranchAsync(client, restaurant.Id.Value);
        var tableId = await CreateTableAsync(client, restaurant.Id.Value, branchId);
        using var publication = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches/{branchId}/category-publications",
            new { Publications = new[] { new { CategoryId = category.Id.Value, IsPublished = true, DisplayOrderOverride = (int?)null } } });
        publication.EnsureSuccessStatusCode();
        using var taxRule = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurant.Id.Value}/branches/{branchId}/item-tax-rules/{item.Id.Value}",
            new { RateBasisPoints = 1000, Behavior = 1, ExpectedVersion = 0 });
        taxRule.EnsureSuccessStatusCode();
        var token = await StartSessionAsync(client, restaurant.Id.Value, branchId, tableId);

        using var firstRequest = CreateRequest(token, "order-key", item.Id.Value, variant.Id.Value, 2);
        using var first = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(17.50m, body.GetProperty("subtotalAmount").GetDecimal());
        Assert.Equal(1.75m, body.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(19.25m, body.GetProperty("totalAmount").GetDecimal());
        Assert.Equal("EUR", body.GetProperty("currency").GetString());
        Assert.False(body.GetProperty("isReplay").GetBoolean());
        var orderId = body.GetProperty("orderId").GetGuid();
        var persisted = await factory.FindOrderAsync(orderId);
        Assert.NotNull(persisted);
        Assert.Equal(restaurant.Id.Value, persisted.RestaurantId);
        Assert.Equal(branchId, persisted.BranchId);
        Assert.Equal(tableId, persisted.DiningTableId);
        Assert.Equal("Server burger", persisted.Lines.Single().ItemName);
        Assert.Equal(8.75m, persisted.Lines.Single().UnitPriceAmount);
        Assert.Equal(1.75m, persisted.Lines.Single().TaxAmount);
        Assert.Equal(1000, persisted.Lines.Single().TaxRateBasisPoints);

        using var replayRequest = CreateRequest(token, "order-key", item.Id.Value, variant.Id.Value, 2);
        using var replay = await client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orderId, replayBody.GetProperty("orderId").GetGuid());
        Assert.True(replayBody.GetProperty("isReplay").GetBoolean());
        Assert.Equal(1, await factory.CountOrdersAsync());

        using var changedRequest = CreateRequest(token, "order-key", item.Id.Value, variant.Id.Value, 3);
        using var changed = await client.SendAsync(changedRequest);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);

        var otherToken = await StartSessionAsync(client, restaurant.Id.Value, branchId, tableId);
        using var wrongSessionRead = new HttpRequestMessage(HttpMethod.Get, $"/api/public/orders/{orderId}");
        wrongSessionRead.Headers.Add(DiningSessionEndpoints.HeaderName, otherToken);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(wrongSessionRead)).StatusCode);

        using var read = new HttpRequestMessage(HttpMethod.Get, $"/api/public/orders/{orderId}");
        read.Headers.Add(DiningSessionEndpoints.HeaderName, token);
        using var readResponse = await client.SendAsync(read);
        readResponse.EnsureSuccessStatusCode();
        Assert.Equal(19.25m, (await readResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("totalAmount").GetDecimal());

        using var cancel = new HttpRequestMessage(HttpMethod.Post, $"/api/public/orders/{orderId}/cancel")
        { Content = JsonContent.Create(new { ExpectedVersion = 1, Reason = "Changed our minds" }) };
        cancel.Headers.Add(DiningSessionEndpoints.HeaderName, token);
        using var cancelResponse = await client.SendAsync(cancel);
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await factory.FindOrderAsync(orderId);
        Assert.NotNull(cancelled);
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(OrderActorType.Guest, cancelled.StatusHistory.Last().ChangedByType);
        Assert.Equal("Changed our minds", cancelled.StatusHistory.Last().Reason);
    }

    [Fact]
    public async Task MissingCapabilityKeyAndUnavailableItemShouldBeRejected()
    {
        await factory.MigrateDatabaseAsync(); await factory.MigrateCatalogDatabaseAsync(); await factory.MigrateOrderingDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync("Rejected orders", DateTimeOffset.UtcNow);
        var category = await factory.SeedMenuCategoryAsync(restaurant.Id.Value, "Meals", 0, isPublished: true);
        var item = await factory.SeedMenuItemAsync(restaurant.Id.Value, category.Id, "Soup", 4m, "EUR", 0, isPublished: true);
        var variant = await factory.FindDefaultMenuItemVariantAsync(item.Id.Value); Assert.NotNull(variant);
        using var client = factory.CreateClient(); var branchId = await CreateBranchAsync(client, restaurant.Id.Value);
        var tableId = await CreateTableAsync(client, restaurant.Id.Value, branchId);
        using var publication = await client.PutAsJsonAsync($"/api/restaurants/{restaurant.Id.Value}/branches/{branchId}/category-publications",
            new { Publications = new[] { new { CategoryId = category.Id.Value, IsPublished = true, DisplayOrderOverride = (int?)null } } });
        publication.EnsureSuccessStatusCode(); var token = await StartSessionAsync(client, restaurant.Id.Value, branchId, tableId);

        using var noCapability = await client.PostAsJsonAsync("/api/public/orders", new { Lines = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Unauthorized, noCapability.StatusCode);
        using var noKeyRequest = CreateRequest(token, null, item.Id.Value, variant.Id.Value, 1);
        using var noKey = await client.SendAsync(noKeyRequest); Assert.Equal(HttpStatusCode.BadRequest, noKey.StatusCode);
        await factory.SetMenuItemAvailabilityAsync(item.Id, false);
        using var unavailableRequest = CreateRequest(token, "unavailable", item.Id.Value, variant.Id.Value, 1);
        using var unavailable = await client.SendAsync(unavailableRequest);
        Assert.Equal(HttpStatusCode.Conflict, unavailable.StatusCode);

        await factory.SetMenuItemAvailabilityAsync(item.Id, true);
        await factory.SetMenuItemPublicationAsync(item.Id, false);
        using var unpublishedRequest = CreateRequest(token, "unpublished", item.Id.Value, variant.Id.Value, 1);
        using var unpublished = await client.SendAsync(unpublishedRequest);
        Assert.Equal(HttpStatusCode.Conflict, unpublished.StatusCode);

        await factory.SetMenuItemPublicationAsync(item.Id, true);
        var otherBranchId = await CreateBranchAsync(client, restaurant.Id.Value);
        var otherTableId = await CreateTableAsync(client, restaurant.Id.Value, otherBranchId);
        var otherToken = await StartSessionAsync(client, restaurant.Id.Value, otherBranchId, otherTableId);
        using var wrongBranchRequest = CreateRequest(otherToken, "wrong-branch", item.Id.Value, variant.Id.Value, 1);
        using var wrongBranch = await client.SendAsync(wrongBranchRequest);
        Assert.Equal(HttpStatusCode.Conflict, wrongBranch.StatusCode);
        Assert.Equal(0, await factory.CountOrdersAsync());
    }

    private static HttpRequestMessage CreateRequest(string token, string? key, Guid itemId, Guid variantId, int quantity)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/orders")
        {
            Content = JsonContent.Create(new { RestaurantId = Guid.NewGuid(), BranchId = Guid.NewGuid(),
                DiningTableId = Guid.NewGuid(), CustomerNote = "guest note", Lines = new[]
                { new { MenuItemId = itemId, VariantId = variantId, Quantity = quantity, Note = "line note", UnitPriceAmount = 0.01m, Currency = "USD" } } })
        };
        request.Headers.Add(DiningSessionEndpoints.HeaderName, token);
        if (key is not null) request.Headers.Add(OrderEndpoints.IdempotencyHeaderName, key);
        return request;
    }

    private static async Task<Guid> CreateBranchAsync(HttpClient client, Guid restaurantId)
    {
        using var response = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/branches", new { Name = $"Branch {Guid.NewGuid():N}" });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static async Task<Guid> CreateTableAsync(HttpClient client, Guid restaurantId, Guid branchId)
    {
        using var response = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/branches/{branchId}/tables", new { Number = 7, DisplayName = "Window 7" });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static async Task<string> StartSessionAsync(HttpClient client, Guid restaurantId, Guid branchId, Guid tableId)
    {
        using var codeResponse = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/public-menu-codes",
            new { BranchId = branchId, DiningTableId = tableId, Purpose = 2 });
        codeResponse.EnsureSuccessStatusCode(); var code = (await codeResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        using var response = await client.PostAsync($"/api/public/menu-codes/{code}/sessions", null);
        response.EnsureSuccessStatusCode(); return Assert.Single(response.Headers.GetValues(DiningSessionEndpoints.HeaderName));
    }
}
