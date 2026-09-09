using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;

public sealed record PublicBranchMenuResponse(
    Guid RestaurantId,
    string RestaurantName,
    Guid BranchId,
    string BranchName,
    IReadOnlyList<PublicMenuCategoryResponse> Categories,
    string? RestaurantSlug,
    string? BranchSlug,
    string? AddressLine,
    string? CityName,
    string? RegionName,
    string? PostalCode,
    string? CountryCode,
    string? Description = null,
    string? About = null,
    string? RestaurantAddress = null,
    string? WebsiteUrl = null,
    string? InstagramUrl = null,
    string? FacebookUrl = null,
    string? WhatsAppUrl = null,
    string? TelegramUrl = null,
    string? TwitterUrl = null,
    string DefaultCurrency = "USD",
    string DefaultLocale = "en-US",
    string TimeZoneId = "Etc/UTC",
    string? LogoUrl = null,
    string? CoverUrl = null);
