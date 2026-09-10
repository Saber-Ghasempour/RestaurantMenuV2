using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Api.FunctionalTests.Infrastructure;
using RestaurantMenu.Notifications.Application;
using RestaurantMenu.Notifications.Presentation;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Restaurants.Domain.Memberships;

namespace RestaurantMenu.Api.FunctionalTests.Notifications;

public sealed class RealtimeNotificationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task GuestJoinShouldAuthorizeExactOrderAndIsolateGroupsAcrossReconnect()
    {
        await factory.MigrateOrderingDatabaseAsync();
        var restaurantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var first = await factory.SeedGuestOrderAsync(restaurantId, branchId);
        var second = await factory.SeedGuestOrderAsync(restaurantId, branchId);
        await using var firstConnection = GuestConnection(first.Token);
        await using var secondConnection = GuestConnection(second.Token);
        var received = new TaskCompletionSource<OrderRealtimeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var leaked = new TaskCompletionSource<OrderRealtimeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        firstConnection.On<OrderRealtimeNotification>("OrderUpdated", received.SetResult);
        secondConnection.On<OrderRealtimeNotification>("OrderUpdated", leaked.SetResult);
        await firstConnection.StartAsync();
        await secondConnection.StartAsync();

        var snapshot = await firstConnection.InvokeAsync<GuestOrderDetail>(
            "JoinOrder", first.Order.Id.Value);
        Assert.Equal(first.Order.Id.Value, snapshot.OrderId);
        await Assert.ThrowsAsync<HubException>(() => secondConnection.InvokeAsync<GuestOrderDetail>(
            "JoinOrder", first.Order.Id.Value));
        await secondConnection.InvokeAsync<GuestOrderDetail>("JoinOrder", second.Order.Id.Value);
        var notification = new OrderRealtimeNotification(Guid.NewGuid(),
            "ordering.order-status-changed", first.Order.Id.Value, "Accepted", 2,
            DateTimeOffset.UtcNow);
        await factory.Services.GetRequiredService<IRealtimeNotifier>()
            .NotifyGuestOrderAsync(notification, CancellationToken.None);

        Assert.Equal(notification, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.NotEqual(leaked.Task, await Task.WhenAny(leaked.Task,
            Task.Delay(TimeSpan.FromMilliseconds(300))));

        await firstConnection.StopAsync();
        await factory.AcceptOrderAsync(first.Order.Id.Value);
        await using var reconnected = GuestConnection(first.Token);
        await reconnected.StartAsync();
        var recovered = await reconnected.InvokeAsync<GuestOrderDetail>(
            "JoinOrder", first.Order.Id.Value);
        Assert.Equal("Accepted", recovered.Status);
        Assert.Equal(snapshot.Version + 1, recovered.Version);
    }

    [Fact]
    public async Task StaffJoinShouldRequirePermissionAndActiveExactBranchAssignment()
    {
        await factory.MigrateDatabaseAsync();
        var restaurant = await factory.SeedRestaurantAsync($"Realtime {Guid.NewGuid():N}",
            DateTimeOffset.UtcNow);
        using var owner = factory.CreateClient();
        var branchId = await CreateBranchAsync(owner, restaurant.Id.Value);
        var otherBranchId = await CreateBranchAsync(owner, restaurant.Id.Value);
        const string subject = "realtime-kitchen";
        await factory.SeedRestaurantMemberAsync(restaurant.Id.Value, subject);
        await factory.SeedBranchMembershipAsync(restaurant.Id.Value, branchId, subject,
            BranchMembershipRole.Kitchen);
        await using var allowed = StaffConnection(subject, Permissions.OrdersRead);
        const string otherSubject = "realtime-other-kitchen";
        await factory.SeedRestaurantMemberAsync(restaurant.Id.Value, otherSubject);
        await factory.SeedBranchMembershipAsync(restaurant.Id.Value, otherBranchId, otherSubject,
            BranchMembershipRole.Kitchen);
        await using var other = StaffConnection(otherSubject, Permissions.OrdersRead);
        var received = new TaskCompletionSource<OrderRealtimeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var leaked = new TaskCompletionSource<OrderRealtimeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        allowed.On<OrderRealtimeNotification>("OrderUpdated", received.SetResult);
        other.On<OrderRealtimeNotification>("OrderUpdated", leaked.SetResult);
        await allowed.StartAsync();
        await other.StartAsync();

        await allowed.InvokeAsync("JoinBranch", restaurant.Id.Value, branchId);
        await other.InvokeAsync("JoinBranch", restaurant.Id.Value, otherBranchId);
        await Assert.ThrowsAsync<HubException>(() => allowed.InvokeAsync("JoinBranch",
            restaurant.Id.Value, otherBranchId));
        var notification = new OrderRealtimeNotification(Guid.NewGuid(),
            "ordering.order-placed", Guid.NewGuid(), "Placed", 1, DateTimeOffset.UtcNow);
        await factory.Services.GetRequiredService<IRealtimeNotifier>()
            .NotifyStaffBranchAsync(restaurant.Id.Value, branchId, notification,
                CancellationToken.None);
        Assert.Equal(notification, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.NotEqual(leaked.Task, await Task.WhenAny(leaked.Task,
            Task.Delay(TimeSpan.FromMilliseconds(300))));

        await using var missingPermission = StaffConnection(subject, "none");
        await Assert.ThrowsAnyAsync<Exception>(() => missingPermission.StartAsync());
    }

    private HubConnection GuestConnection(string token) => Connection(
        GuestOrderNotificationsHub.Path,
        new Dictionary<string, string> { ["X-Dining-Session"] = token });

    private HubConnection StaffConnection(string subject, string permission) => Connection(
        StaffOrderNotificationsHub.Path,
        new Dictionary<string, string>
        {
            [TestAuthenticationHandler.SubjectHeader] = subject,
            [TestAuthenticationHandler.PermissionsHeader] = permission
        });

    private HubConnection Connection(string path, IDictionary<string, string> headers) =>
        new HubConnectionBuilder().WithUrl(new Uri(factory.Server.BaseAddress, path), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            foreach (var header in headers) options.Headers.Add(header.Key, header.Value);
        }).Build();

    private static async Task<Guid> CreateBranchAsync(HttpClient client, Guid restaurantId)
    {
        using var response = await client.PostAsJsonAsync($"/api/restaurants/{restaurantId}/branches",
            new { Name = $"Realtime {Guid.NewGuid():N}" });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }
}
