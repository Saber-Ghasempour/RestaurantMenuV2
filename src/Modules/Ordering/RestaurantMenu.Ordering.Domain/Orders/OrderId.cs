namespace RestaurantMenu.Ordering.Domain.Orders;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}
