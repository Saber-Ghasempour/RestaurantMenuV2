using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.Orders;

public sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public void Add(Order order) => dbContext.Orders.Add(order);
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        dbContext.Orders.AsNoTracking().Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
    public Task<Order?> GetForUpdateAsync(Guid restaurantId, Guid branchId, OrderId id,
        CancellationToken cancellationToken) => dbContext.Orders
        .Include(order => order.StatusHistory)
        .SingleOrDefaultAsync(order => order.Id == id && order.RestaurantId == restaurantId &&
            order.BranchId == branchId, cancellationToken);
}
