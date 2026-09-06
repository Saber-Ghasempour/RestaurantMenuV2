namespace RestaurantMenu.Catalog.Application.Items.GetMenuItem;

public sealed record MenuItemResponse(
    Guid Id,
    Guid RestaurantId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int DisplayOrder,
    bool IsAvailable,
    DateTimeOffset CreatedAtUtc,
    long Version);
