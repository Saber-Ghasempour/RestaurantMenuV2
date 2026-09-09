using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Ordering.Domain.Orders;

public sealed record OrderPlacedDomainEvent(OrderId OrderId, Guid RestaurantId, Guid BranchId,
    Guid DiningSessionId, decimal TotalAmount, string Currency, DateTimeOffset OccurredAtUtc) : IDomainEvent;
