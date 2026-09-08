namespace RestaurantMenu.Restaurants.Domain.DiningTables;

public readonly record struct DiningTableId(Guid Value)
{
    public static DiningTableId New() => new(Guid.NewGuid());
}
