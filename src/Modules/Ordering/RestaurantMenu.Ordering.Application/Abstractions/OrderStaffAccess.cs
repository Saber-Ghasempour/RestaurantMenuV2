namespace RestaurantMenu.Ordering.Application.Abstractions;

public enum OrderStaffRole
{
    Manager,
    Cashier,
    Kitchen,
    Waiter
}

public interface IOrderStaffAccessProvider
{
    Task<OrderStaffRole?> GetActiveRoleAsync(Guid restaurantId, Guid branchId,
        string subject, CancellationToken cancellationToken);
}
