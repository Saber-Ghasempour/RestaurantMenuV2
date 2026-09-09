namespace RestaurantMenu.Catalog.Application.Variants;

public sealed record MenuItemVariantResponse(
    Guid Id,
    Guid RestaurantId,
    Guid MenuItemId,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int DisplayOrder,
    bool IsDefault,
    bool IsAvailable,
    DateTimeOffset CreatedAtUtc,
    long Version);
