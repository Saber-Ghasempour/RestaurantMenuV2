using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Restaurants.Domain.Memberships;

namespace RestaurantMenu.Api.FunctionalTests.Ordering;

public sealed class StaffOrderEndpointsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task StaffRolesShouldCompleteWorkflowAndWriteAttributedTimeline()
    {
        var setup = await SetupAsync();
        await AddStaffAsync(setup.RestaurantId, setup.BranchId, "cashier", BranchMembershipRole.Cashier);
        await AddStaffAsync(setup.RestaurantId, setup.BranchId, "kitchen", BranchMembershipRole.Kitchen);
        await AddStaffAsync(setup.RestaurantId, setup.BranchId, "waiter", BranchMembershipRole.Waiter);
        var order = await factory.SeedOrderAsync(setup.RestaurantId, setup.BranchId);
        using var client = factory.CreateClient();

        await AssertOkAsync(client, setup, order.Id.Value, "accept", 1, "cashier", "orders.accept");
        await AssertOkAsync(client, setup, order.Id.Value, "start-preparing", 2, "kitchen", "orders.prepare");
        await AssertOkAsync(client, setup, order.Id.Value, "ready", 3, "kitchen", "orders.prepare");
        await AssertOkAsync(client, setup, order.Id.Value, "served", 4, "waiter", "orders.serve");
        await AssertOkAsync(client, setup, order.Id.Value, "complete", 5, "cashier", "orders.complete");

        using var timelineRequest = Request(HttpMethod.Get,
            $"/api/restaurants/{setup.RestaurantId}/branches/{setup.BranchId}/orders/{order.Id.Value}/timeline",
            "cashier", "orders.read");
        using var timelineResponse = await client.SendAsync(timelineRequest);
        timelineResponse.EnsureSuccessStatusCode();
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6, timeline.GetArrayLength());
        Assert.Equal("Guest", timeline[0].GetProperty("changedByType").GetString());
        Assert.Equal("cashier", timeline[1].GetProperty("changedBySubject").GetString());
        Assert.Equal("Completed", timeline[5].GetProperty("toStatus").GetString());
    }

    [Fact]
    public async Task TransitionShouldEnforcePermissionRoleVersionAndBranchScope()
    {
        var setup = await SetupAsync();
        var otherBranchId = await CreateBranchAsync(setup.OwnerClient, setup.RestaurantId);
        await AddStaffAsync(setup.RestaurantId, setup.BranchId, "cashier-guard", BranchMembershipRole.Cashier);
        await factory.SeedRestaurantMemberAsync(setup.RestaurantId, "unassigned-staff");
        await AddStaffAsync(setup.RestaurantId, setup.BranchId, "suspended-staff", BranchMembershipRole.Cashier);
        using (var suspend = await setup.OwnerClient.PutAsJsonAsync(
            $"/api/restaurants/{setup.RestaurantId}/branches/{setup.BranchId}/members/suspended-staff",
            new { Role = BranchMembershipRole.Cashier, Status = BranchMembershipStatus.Suspended,
                ExpectedVersion = 1 }))
            suspend.EnsureSuccessStatusCode();
        var order = await factory.SeedOrderAsync(setup.RestaurantId, setup.BranchId);
        var otherOrder = await factory.SeedOrderAsync(setup.RestaurantId, otherBranchId);
        using var client = factory.CreateClient();

        using var missingPermission = Request(HttpMethod.Post,
            OrderAction(setup, order.Id.Value, "accept"), "cashier-guard", "none", new { ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(missingPermission)).StatusCode);

        using var wrongRole = Request(HttpMethod.Post,
            OrderAction(setup, order.Id.Value, "start-preparing"), "cashier-guard", "orders.prepare", new { ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(wrongRole)).StatusCode);

        using var noAssignment = Request(HttpMethod.Post,
            OrderAction(setup, order.Id.Value, "accept"), "unassigned-staff", "orders.accept", new { ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(noAssignment)).StatusCode);

        using var suspended = Request(HttpMethod.Post,
            OrderAction(setup, order.Id.Value, "accept"), "suspended-staff", "orders.accept", new { ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(suspended)).StatusCode);

        using var stale = Request(HttpMethod.Post,
            OrderAction(setup, order.Id.Value, "accept"), "cashier-guard", "orders.accept", new { ExpectedVersion = 99 });
        Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(stale)).StatusCode);

        using var crossBranch = Request(HttpMethod.Post,
            OrderAction(setup, otherOrder.Id.Value, "accept"), "cashier-guard", "orders.accept", new { ExpectedVersion = 1 });
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(crossBranch)).StatusCode);
    }

    [Fact]
    public async Task KitchenQueueShouldContainOnlyBranchAcceptedAndPreparingOrders()
    {
        var setup = await SetupAsync();
        var otherBranchId = await CreateBranchAsync(setup.OwnerClient, setup.RestaurantId);
        await AddStaffAsync(setup.RestaurantId, setup.BranchId, "kitchen-queue", BranchMembershipRole.Kitchen);
        var accepted = await factory.SeedOrderAsync(setup.RestaurantId, setup.BranchId, OrderStatus.Accepted);
        var preparing = await factory.SeedOrderAsync(setup.RestaurantId, setup.BranchId, OrderStatus.Preparing);
        await factory.SeedOrderAsync(setup.RestaurantId, setup.BranchId, OrderStatus.Ready);
        await factory.SeedOrderAsync(setup.RestaurantId, otherBranchId, OrderStatus.Accepted);
        using var client = factory.CreateClient();
        using var request = Request(HttpMethod.Get,
            $"/api/restaurants/{setup.RestaurantId}/branches/{setup.BranchId}/orders/queues/kitchen",
            "kitchen-queue", "orders.read");
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var queue = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, queue.GetArrayLength());
        var ids = queue.EnumerateArray().Select(value => value.GetProperty("id").GetGuid()).ToArray();
        Assert.Contains(accepted.Id.Value, ids);
        Assert.Contains(preparing.Id.Value, ids);
    }

    private async Task<(Guid RestaurantId, Guid BranchId, HttpClient OwnerClient)> SetupAsync()
    {
        await factory.MigrateDatabaseAsync();
        await factory.MigrateOrderingDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync($"B3 {Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        var ownerClient = factory.CreateClient();
        var branchId = await CreateBranchAsync(ownerClient, restaurant.Id.Value);
        return (restaurant.Id.Value, branchId, ownerClient);
    }

    private async Task AddStaffAsync(Guid restaurantId, Guid branchId, string subject, BranchMembershipRole role)
    {
        await factory.SeedRestaurantMemberAsync(restaurantId, subject);
        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            $"/api/restaurants/{restaurantId}/branches/{branchId}/members/{subject}",
            new { Role = role, Status = BranchMembershipStatus.Active });
        response.EnsureSuccessStatusCode();
    }

    private static async Task AssertOkAsync(HttpClient client,
        (Guid RestaurantId, Guid BranchId, HttpClient OwnerClient) setup, Guid orderId,
        string action, long version, string subject, string permission)
    {
        using var request = Request(HttpMethod.Post, OrderAction(setup, orderId, action),
            subject, permission, new { ExpectedVersion = version });
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static string OrderAction((Guid RestaurantId, Guid BranchId, HttpClient OwnerClient) setup,
        Guid orderId, string action) =>
        $"/api/restaurants/{setup.RestaurantId}/branches/{setup.BranchId}/orders/{orderId}/{action}";

    private static HttpRequestMessage Request(HttpMethod method, string uri, string subject,
        string permission, object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add(TestAuthenticationHandler.SubjectHeader, subject);
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, permission);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<Guid> CreateBranchAsync(HttpClient client, Guid restaurantId)
    {
        using var response = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/branches",
            new { Name = $"Branch {Guid.NewGuid():N}" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}
