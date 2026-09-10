using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Ordering.Domain.Orders;

public sealed class OrderStatusHistory : Entity<Guid>
{
    public const int MaxSubjectLength = 255;
    public const int MaxReasonLength = 500;

    private OrderStatusHistory() : base(default) { }
    internal OrderStatusHistory(Guid id, OrderId orderId, OrderStatus? fromStatus,
        OrderStatus toStatus, OrderActorType changedByType, string? changedBySubject,
        string? reason, DateTimeOffset createdAtUtc) : base(id)
    {
        OrderId = orderId; FromStatus = fromStatus; ToStatus = toStatus;
        ChangedByType = changedByType; ChangedBySubject = changedBySubject;
        Reason = reason; CreatedAtUtc = createdAtUtc;
    }

    public OrderId OrderId { get; }
    public OrderStatus? FromStatus { get; }
    public OrderStatus ToStatus { get; }
    public OrderActorType ChangedByType { get; }
    public string? ChangedBySubject { get; }
    public string? Reason { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}
