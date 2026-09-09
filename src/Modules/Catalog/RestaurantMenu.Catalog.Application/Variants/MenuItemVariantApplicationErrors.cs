using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Variants;

public static class MenuItemVariantApplicationErrors
{
    public static ErrorDetail MenuItemNotFound(MenuItemId id) => ErrorDetail.NotFound(
        "Catalog.MenuItemNotFound", $"Menu item '{id.Value}' was not found.");

    public static ErrorDetail VariantNotFound(MenuItemVariantId id) => ErrorDetail.NotFound(
        "Catalog.MenuItemVariantNotFound", $"Menu item variant '{id.Value}' was not found.");

    public static ErrorDetail VersionConflict(MenuItemVariantId id) => ErrorDetail.Conflict(
        "Catalog.MenuItemVariantVersionConflict",
        $"Menu item variant '{id.Value}' was modified by another request.");

    public static readonly ErrorDetail DefaultRequired = ErrorDetail.Conflict(
        "Catalog.MenuItemVariantDefaultRequired",
        "The first active variant for a menu item must be the default.");

    public static readonly ErrorDetail DuplicateName = ErrorDetail.Conflict(
        "Catalog.MenuItemVariantNameConflict",
        "An active variant with this name already exists for the menu item.");

    public static readonly ErrorDetail CurrencyMismatch = ErrorDetail.Validation(
        "Catalog.MenuItemVariantCurrencyMismatch",
        "All variants for a menu item must use the same currency.");
}
