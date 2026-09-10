using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Notifications.Application;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Infrastructure.Messaging;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Notifications.IntegrationTests;

public sealed class NotificationConsumerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task OrderPlacedShouldNotifyOnlyItsStaffBranch()
    {
        var notifier = new RecordingNotifier();
        var value = Placed();
        var envelope = Envelope(value.EventId, value.OrderId, value.OrderVersion,
            "ordering.order-placed", value);

        await new OrderPlacedNotificationConsumer(notifier)
            .HandleAsync(envelope, CancellationToken.None);

        var staff = Assert.Single(notifier.Staff);
        Assert.Equal((value.RestaurantId, value.BranchId),
            (staff.RestaurantId, staff.BranchId));
        Assert.Equal("Placed", staff.Notification.Status);
        Assert.Empty(notifier.Guests);
    }

    [Fact]
    public async Task StatusChangeShouldNotifyOnlyItsGuestOrderAndStaffBranch()
    {
        var notifier = new RecordingNotifier();
        var value = StatusChanged();
        var envelope = Envelope(value.EventId, value.OrderId, value.OrderVersion,
            "ordering.order-status-changed", value);

        await new OrderStatusChangedNotificationConsumer(notifier)
            .HandleAsync(envelope, CancellationToken.None);

        Assert.Equal(value.OrderId, Assert.Single(notifier.Guests).OrderId);
        var staff = Assert.Single(notifier.Staff);
        Assert.Equal((value.RestaurantId, value.BranchId),
            (staff.RestaurantId, staff.BranchId));
        Assert.Equal("Accepted", staff.Notification.Status);
    }

    [Fact]
    public async Task InboxShouldSuppressDuplicateRealtimeEffect()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;
        await using var context = new OrderingDbContext(options);
        await context.Database.MigrateAsync();
        var notifier = new RecordingNotifier();
        var value = StatusChanged();
        var envelope = Envelope(value.EventId, value.OrderId, value.OrderVersion,
            "ordering.order-status-changed", value);
        var consumer = new OrderStatusChangedNotificationConsumer(notifier);
        var processor = new InboxProcessor(context, new MessagingOptions("amqp://unused",
            "test", 10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 3,
            TimeSpan.FromMinutes(1)), TimeProvider.System);

        Assert.Equal(InboxProcessingOutcome.Processed,
            await processor.ProcessAsync(envelope, consumer, CancellationToken.None));
        Assert.Equal(InboxProcessingOutcome.Duplicate,
            await processor.ProcessAsync(envelope, consumer, CancellationToken.None));
        Assert.Single(notifier.Guests);
        Assert.Single(notifier.Staff);
        Assert.Equal(1, await context.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task ConsumerShouldRejectPayloadThatDoesNotMatchItsEnvelope()
    {
        var value = StatusChanged();
        var envelope = Envelope(value.EventId, Guid.NewGuid(), value.OrderVersion,
            "ordering.order-status-changed", value);

        await Assert.ThrowsAsync<JsonException>(() =>
            new OrderStatusChangedNotificationConsumer(new RecordingNotifier())
                .HandleAsync(envelope, CancellationToken.None));
    }

    private static OrderPlacedV1 Placed() => new(Guid.NewGuid(), DateTimeOffset.UtcNow,
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 12m, "EUR", 1);

    private static OrderStatusChangedV1 StatusChanged() => new(Guid.NewGuid(),
        DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Placed",
        "Accepted", 2);

    private static IntegrationEventEnvelope Envelope<T>(Guid eventId, Guid orderId,
        long orderVersion, string name, T value) => new(eventId, name, 1, orderId,
            orderVersion, DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(value, JsonOptions));

    private sealed class RecordingNotifier : IRealtimeNotifier
    {
        public List<OrderRealtimeNotification> Guests { get; } = [];
        public List<(Guid RestaurantId, Guid BranchId,
            OrderRealtimeNotification Notification)> Staff { get; } = [];
        public Task NotifyGuestOrderAsync(OrderRealtimeNotification notification,
            CancellationToken cancellationToken)
        { Guests.Add(notification); return Task.CompletedTask; }
        public Task NotifyStaffBranchAsync(Guid restaurantId, Guid branchId,
            OrderRealtimeNotification notification, CancellationToken cancellationToken)
        { Staff.Add((restaurantId, branchId, notification)); return Task.CompletedTask; }
    }
}
