using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed class MenuItemMedia
{
    public const int MaxAltTextLength = 200;
    private MenuItemMedia() { AltText = null; }
    private MenuItemMedia(Guid restaurantId, MenuItemId menuItemId, Guid mediaAssetId,
        int displayOrder, string? altText, bool isPrimary)
    { RestaurantId = restaurantId; MenuItemId = menuItemId; MediaAssetId = mediaAssetId; DisplayOrder = displayOrder; AltText = altText; IsPrimary = isPrimary; }
    public Guid RestaurantId { get; private set; }
    public MenuItemId MenuItemId { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public static Result<MenuItemMedia> Create(Guid restaurantId, MenuItemId menuItemId,
        Guid mediaAssetId, int displayOrder, string? altText, bool isPrimary)
    {
        if (restaurantId == Guid.Empty || menuItemId.Value == Guid.Empty || mediaAssetId == Guid.Empty)
            return Result.Failure<MenuItemMedia>(ErrorDetail.Validation("Catalog.InvalidMenuItemMediaOwner", "Restaurant, menu item, and media identifiers are required."));
        if (displayOrder < 0) return Result.Failure<MenuItemMedia>(ErrorDetail.Validation("Catalog.InvalidMenuItemMediaOrder", "Media display order cannot be negative."));
        var normalized = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        if (normalized?.Length > MaxAltTextLength) return Result.Failure<MenuItemMedia>(ErrorDetail.Validation("Catalog.MenuItemMediaAltTextTooLong", $"Alt text must not exceed {MaxAltTextLength} characters."));
        return Result.Success(new MenuItemMedia(restaurantId, menuItemId, mediaAssetId, displayOrder, normalized, isPrimary));
    }
}
