namespace RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;

public sealed record MenuCategoryResponse(
    Guid Id,
    Guid RestaurantId,
    Guid? ParentCategoryId,
    string Name,
    int DisplayOrder,
    DateTimeOffset CreatedAtUtc,
    long Version);
