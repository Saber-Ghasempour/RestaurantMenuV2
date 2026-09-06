namespace RestaurantMenu.Catalog.Domain.Categories;

public readonly record struct MenuCategoryId(Guid Value)
{
    public static MenuCategoryId New() =>
        new(Guid.CreateVersion7());
}
