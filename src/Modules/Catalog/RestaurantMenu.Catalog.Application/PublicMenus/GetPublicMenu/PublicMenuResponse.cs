namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

public sealed record PublicMenuResponse(
    Guid RestaurantId,
    string RestaurantName,
    IReadOnlyList<PublicMenuCategoryResponse> Categories,
    string? Description = null,
    string? About = null,
    string? Address = null);

public sealed record PublicMenuCategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    int DisplayOrder,
    IReadOnlyList<PublicMenuItemResponse> Items);

public sealed record PublicMenuItemResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int DisplayOrder);
