namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

public sealed record PublicMenuResponse(
    Guid RestaurantId,
    string RestaurantName,
    IReadOnlyList<PublicMenuCategoryResponse> Categories,
    string? Description = null,
    string? About = null,
    string? Address = null,
    string? WebsiteUrl = null,
    string? InstagramUrl = null,
    string? FacebookUrl = null,
    string? WhatsAppUrl = null,
    string? TelegramUrl = null,
    string? TwitterUrl = null,
    string? Slug = null,
    string DefaultCurrency = "USD",
    string DefaultLocale = "en-US",
    string TimeZoneId = "Etc/UTC");

public sealed record PublicMenuCategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    int DisplayOrder,
    IReadOnlyList<PublicMenuItemResponse> Items,
    string? Description = null);

public sealed record PublicMenuItemResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int DisplayOrder,
    bool IsAvailable = true,
    string? Recipe = null,
    int? Calories = null,
    IReadOnlyList<string>? Tags = null,
    string? AllergenNotes = null,
    short? PreparationTimeMinutes = null,
    bool IsFeatured = false);
