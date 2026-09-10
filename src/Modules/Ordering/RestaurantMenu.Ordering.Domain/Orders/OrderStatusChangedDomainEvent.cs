using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Ordering.Domain.Orders;

public sealed record OrderStatusChangedDomainEvent(OrderId OrderId, Guid RestaurantId,
    Guid BranchId, OrderStatus FromStatus, OrderStatus ToStatus,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
