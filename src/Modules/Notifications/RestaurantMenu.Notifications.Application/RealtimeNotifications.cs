using System.Text.Json;
using RestaurantMenu.Ordering.Application.Abstractions;

namespace RestaurantMenu.Notifications.Application;

public sealed record OrderRealtimeNotification(Guid EventId, string EventName,
    Guid OrderId, string Status, long OrderVersion, DateTimeOffset OccurredAtUtc);

public interface IRealtimeNotifier
{
    Task NotifyGuestOrderAsync(OrderRealtimeNotification notification,
        CancellationToken cancellationToken);
    Task NotifyStaffBranchAsync(Guid restaurantId, Guid branchId,
        OrderRealtimeNotification notification, CancellationToken cancellationToken);
}

public sealed class OrderPlacedNotificationConsumer(IRealtimeNotifier notifier)
    : IIntegrationEventConsumer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string ConsumerName => "notifications.order-placed-v1";
    public string EventName => "ordering.order-placed";
    public int EventVersion => 1;

    public async Task HandleAsync(IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Deserialize<OrderPlacedV1>(envelope.Payload, JsonOptions)
            ?? throw new JsonException("The OrderPlacedV1 payload is empty.");
        if (value.EventId != envelope.Id || value.OrderId != envelope.AggregateId ||
            value.OrderVersion != envelope.AggregateVersion)
            throw new JsonException("The OrderPlacedV1 payload does not match its envelope.");
        await notifier.NotifyStaffBranchAsync(value.RestaurantId, value.BranchId,
            new(value.EventId, envelope.Name, value.OrderId, "Placed", value.OrderVersion,
                value.OccurredAtUtc), cancellationToken);
    }
}

public sealed class OrderStatusChangedNotificationConsumer(IRealtimeNotifier notifier)
    : IIntegrationEventConsumer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string ConsumerName => "notifications.order-status-changed-v1";
    public string EventName => "ordering.order-status-changed";
    public int EventVersion => 1;

    public async Task HandleAsync(IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Deserialize<OrderStatusChangedV1>(envelope.Payload, JsonOptions)
            ?? throw new JsonException("The OrderStatusChangedV1 payload is empty.");
        if (value.EventId != envelope.Id || value.OrderId != envelope.AggregateId ||
            value.OrderVersion != envelope.AggregateVersion)
            throw new JsonException("The OrderStatusChangedV1 payload does not match its envelope.");
        var notification = new OrderRealtimeNotification(value.EventId, envelope.Name,
            value.OrderId, value.ToStatus, value.OrderVersion, value.OccurredAtUtc);
        await notifier.NotifyGuestOrderAsync(notification, cancellationToken);
        await notifier.NotifyStaffBranchAsync(value.RestaurantId, value.BranchId,
            notification, cancellationToken);
    }
}
