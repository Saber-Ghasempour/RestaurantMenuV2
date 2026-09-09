namespace RestaurantMenu.Ordering.Domain.Orders;

public readonly record struct OrderLineId(Guid Value)
{
    public static OrderLineId New() => new(Guid.CreateVersion7());
}
