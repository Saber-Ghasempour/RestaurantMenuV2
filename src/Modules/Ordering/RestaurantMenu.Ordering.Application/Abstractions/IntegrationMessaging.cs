namespace RestaurantMenu.Ordering.Application.Abstractions;

public sealed record IntegrationEventEnvelope(Guid Id, string Name, int Version,
    Guid AggregateId, long AggregateVersion, DateTimeOffset OccurredAtUtc, string Payload);

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}

public interface IIntegrationEventConsumer
{
    string ConsumerName { get; }
    string EventName { get; }
    int EventVersion { get; }
    Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}

public sealed record OrderPlacedV1(Guid EventId, DateTimeOffset OccurredAtUtc,
    Guid OrderId, Guid RestaurantId, Guid BranchId, Guid DiningSessionId,
    decimal TotalAmount, string Currency, long OrderVersion);

public sealed record OrderStatusChangedV1(Guid EventId, DateTimeOffset OccurredAtUtc,
    Guid OrderId, Guid RestaurantId, Guid BranchId, string FromStatus,
    string ToStatus, long OrderVersion);
