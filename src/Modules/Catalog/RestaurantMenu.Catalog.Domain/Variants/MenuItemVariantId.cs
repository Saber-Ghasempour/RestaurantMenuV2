namespace RestaurantMenu.Catalog.Domain.Variants;

public readonly record struct MenuItemVariantId(Guid Value)
{
    public static MenuItemVariantId New() => new(Guid.CreateVersion7());
}
