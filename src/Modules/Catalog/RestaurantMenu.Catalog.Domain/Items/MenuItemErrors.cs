using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Items;

public static class MenuItemErrors
{
    public static readonly ErrorDetail RestaurantRequired =
        ErrorDetail.Validation(
            "Catalog.MenuItemRestaurantRequired",
            "A restaurant identifier is required.");

    public static readonly ErrorDetail CategoryRequired =
        ErrorDetail.Validation(
            "Catalog.MenuItemCategoryRequired",
            "A menu category identifier is required.");

    public static readonly ErrorDetail NameRequired =
        ErrorDetail.Validation(
            "Catalog.MenuItemNameRequired",
            "The menu item name is required.");

    public static readonly ErrorDetail NameTooLong =
        ErrorDetail.Validation(
            "Catalog.MenuItemNameTooLong",
            $"The menu item name must not exceed {MenuItem.MaxNameLength} characters.");

    public static readonly ErrorDetail DescriptionTooLong =
        ErrorDetail.Validation(
            "Catalog.MenuItemDescriptionTooLong",
            $"The menu item description must not exceed {MenuItem.MaxDescriptionLength} characters.");

    public static readonly ErrorDetail InvalidDisplayOrder =
        ErrorDetail.Validation(
            "Catalog.InvalidMenuItemDisplayOrder",
            "The menu item display order cannot be negative.");

    public static readonly ErrorDetail NegativePrice =
        ErrorDetail.Validation(
            "Catalog.MenuItemNegativePrice",
            "The menu item price cannot be negative.");

    public static readonly ErrorDetail PricePrecisionExceeded =
        ErrorDetail.Validation(
            "Catalog.MenuItemPricePrecisionExceeded",
            "The menu item price cannot have more than two decimal places.");

    public static readonly ErrorDetail CurrencyRequired =
        ErrorDetail.Validation(
            "Catalog.MenuItemCurrencyRequired",
            "A currency code is required.");

    public static readonly ErrorDetail InvalidCurrency =
        ErrorDetail.Validation(
            "Catalog.MenuItemInvalidCurrency",
            "The currency must be a three-letter code.");
}
