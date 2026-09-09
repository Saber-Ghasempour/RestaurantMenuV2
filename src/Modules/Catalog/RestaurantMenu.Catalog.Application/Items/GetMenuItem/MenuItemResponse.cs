using RestaurantMenu.Catalog.Application.Variants;

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
    long Version,
    string? Recipe = null,
    int? Calories = null,
    IReadOnlyList<string>? Tags = null,
    string? AllergenNotes = null,
    short? PreparationTimeMinutes = null,
    bool IsFeatured = false,
    bool IsPublished = false,
    IReadOnlyList<MenuItemVariantResponse>? Variants = null,
    IReadOnlyList<MenuItemMediaResponse>? Media = null);

public sealed record MenuItemMediaResponse(Guid MediaAssetId, int DisplayOrder, string? AltText, bool IsPrimary);
