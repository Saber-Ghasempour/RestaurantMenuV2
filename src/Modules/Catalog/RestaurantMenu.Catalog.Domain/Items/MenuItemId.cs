namespace RestaurantMenu.Catalog.Domain.Items;

public readonly record struct MenuItemId(Guid Value)
{
    public static MenuItemId New() =>
        new(Guid.CreateVersion7());
}
