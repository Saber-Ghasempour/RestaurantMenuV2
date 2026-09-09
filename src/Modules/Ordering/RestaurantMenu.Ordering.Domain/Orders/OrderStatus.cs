namespace RestaurantMenu.Ordering.Domain.Orders;

public enum OrderStatus
{
    Placed,
    Accepted,
    Preparing,
    Ready,
    Served,
    Completed,
    Rejected,
    Cancelled
}
