using System.Text.Json;
using System.Diagnostics;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

internal static class OrderingIntegrationEventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IntegrationEventEnvelope? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        OrderPlacedDomainEvent value => Create("ordering.order-placed", value.OrderId.Value,
            value.OrderVersion, value.OccurredAtUtc, id => new OrderPlacedV1(id,
                value.OccurredAtUtc, value.OrderId.Value, value.RestaurantId, value.BranchId,
                value.DiningSessionId, value.TotalAmount, value.Currency, value.OrderVersion)),
        OrderStatusChangedDomainEvent value => Create("ordering.order-status-changed",
            value.OrderId.Value, value.OrderVersion, value.OccurredAtUtc,
            id => new OrderStatusChangedV1(id, value.OccurredAtUtc, value.OrderId.Value,
                value.RestaurantId, value.BranchId, value.FromStatus.ToString(),
                value.ToStatus.ToString(), value.OrderVersion)),
        _ => null
    };

    private static IntegrationEventEnvelope Create<T>(string name, Guid aggregateId,
        long aggregateVersion, DateTimeOffset occurredAtUtc, Func<Guid, T> factory)
    {
        var id = Guid.CreateVersion7();
        var activity = Activity.Current;
        return new IntegrationEventEnvelope(id, name, 1, aggregateId, aggregateVersion,
            occurredAtUtc, JsonSerializer.Serialize(factory(id), JsonOptions),
            activity?.IdFormat == ActivityIdFormat.W3C ? activity.Id : null,
            activity?.IdFormat == ActivityIdFormat.W3C ? activity.TraceStateString : null);
    }
}
