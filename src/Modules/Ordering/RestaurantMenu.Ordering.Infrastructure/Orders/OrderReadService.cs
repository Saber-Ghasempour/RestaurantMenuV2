using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.Orders;

public sealed class OrderReadService(OrderingDbContext dbContext) : IOrderReadService
{
    public async Task<IReadOnlyList<OrderQueueItem>> ListQueueAsync(Guid restaurantId,
        Guid branchId, IReadOnlyCollection<OrderStatus> statuses, CancellationToken cancellationToken)
    {
        var items = await dbContext.Orders.AsNoTracking()
            .Where(order => order.RestaurantId == restaurantId && order.BranchId == branchId && statuses.Contains(order.Status))
            .OrderBy(order => order.CreatedAtUtc).ThenBy(order => order.Id)
            .Select(order => new { order.Id, order.PublicNumber, order.DiningTableId,
                order.TableDisplayName, order.Status, order.TotalAmount, order.Currency,
                order.CreatedAtUtc, order.Version })
            .ToArrayAsync(cancellationToken);

        return items.Select(order => new OrderQueueItem(order.Id.Value, order.PublicNumber,
            order.DiningTableId, order.TableDisplayName, order.Status.ToString(),
            order.TotalAmount, order.Currency, order.CreatedAtUtc, order.Version)).ToArray();
    }

    public async Task<IReadOnlyList<OrderTimelineEntry>?> GetTimelineAsync(Guid restaurantId,
        Guid branchId, OrderId orderId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Orders.AsNoTracking().AnyAsync(order => order.Id == orderId &&
            order.RestaurantId == restaurantId && order.BranchId == branchId, cancellationToken))
            return null;
        var entries = await dbContext.OrderStatusHistory.AsNoTracking()
            .Where(value => value.OrderId == orderId)
            .OrderBy(value => value.CreatedAtUtc).ThenBy(value => value.Id)
            .Select(value => new { value.Id, value.FromStatus, value.ToStatus,
                value.ChangedByType, value.ChangedBySubject, value.Reason, value.CreatedAtUtc })
            .ToArrayAsync(cancellationToken);

        return entries.Select(value => new OrderTimelineEntry(value.Id,
            value.FromStatus?.ToString(), value.ToStatus.ToString(), value.ChangedByType.ToString(),
            value.ChangedBySubject, value.Reason, value.CreatedAtUtc)).ToArray();
    }

    public async Task<GuestOrderDetail?> GetGuestOrderAsync(Guid restaurantId, Guid branchId,
        Guid diningSessionId, OrderId orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.AsNoTracking().Include(value => value.Lines)
            .SingleOrDefaultAsync(value => value.Id == orderId && value.RestaurantId == restaurantId &&
                value.BranchId == branchId && value.DiningSessionId == diningSessionId, cancellationToken);
        return order is null ? null : new GuestOrderDetail(order.Id.Value, order.PublicNumber,
            order.Status.ToString(), order.TableDisplayName, order.SubtotalAmount, order.TotalAmount,
            order.Currency, order.CustomerNote, order.CreatedAtUtc, order.Version,
            order.Lines.Select(line => new GuestOrderLine(line.MenuItemId, line.VariantId,
                line.ItemName, line.VariantName, line.UnitPriceAmount, line.Currency,
                line.Quantity, line.LineTotalAmount, line.Note)).ToArray());
    }
}
