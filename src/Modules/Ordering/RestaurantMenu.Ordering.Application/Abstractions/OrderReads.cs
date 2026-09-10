using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Application.Abstractions;

public sealed record OrderQueueItem(Guid Id, string PublicNumber, Guid DiningTableId,
    string TableDisplayName, string Status, decimal TotalAmount, string Currency,
    DateTimeOffset CreatedAtUtc, long Version);
public sealed record OrderTimelineEntry(Guid Id, string? FromStatus, string ToStatus,
    string ChangedByType, string? ChangedBySubject, string? Reason, DateTimeOffset CreatedAtUtc);
public sealed record GuestOrderLine(Guid? MenuItemId, Guid? VariantId, string ItemName,
    string? VariantName, decimal UnitPriceAmount, string Currency, int Quantity,
    decimal LineTotalAmount, string? Note);
public sealed record GuestOrderDetail(Guid OrderId, string PublicNumber, string Status,
    string TableDisplayName, decimal SubtotalAmount, decimal TotalAmount, string Currency,
    string? CustomerNote, DateTimeOffset CreatedAtUtc, long Version,
    IReadOnlyList<GuestOrderLine> Lines);

public interface IOrderReadService
{
    Task<IReadOnlyList<OrderQueueItem>> ListQueueAsync(Guid restaurantId, Guid branchId,
        IReadOnlyCollection<OrderStatus> statuses, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderTimelineEntry>?> GetTimelineAsync(Guid restaurantId, Guid branchId,
        OrderId orderId, CancellationToken cancellationToken);
    Task<GuestOrderDetail?> GetGuestOrderAsync(Guid restaurantId, Guid branchId,
        Guid diningSessionId, OrderId orderId, CancellationToken cancellationToken);
}
