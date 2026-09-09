using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Variants;

public static class MenuItemVariantErrors
{
    public static readonly ErrorDetail RestaurantRequired = ErrorDetail.Validation(
        "Catalog.MenuItemVariantRestaurantRequired",
        "A restaurant identifier is required.");

    public static readonly ErrorDetail MenuItemRequired = ErrorDetail.Validation(
        "Catalog.MenuItemVariantMenuItemRequired",
        "A menu item identifier is required.");

    public static readonly ErrorDetail NameRequired = ErrorDetail.Validation(
        "Catalog.MenuItemVariantNameRequired",
        "The variant name is required.");

    public static readonly ErrorDetail NameTooLong = ErrorDetail.Validation(
        "Catalog.MenuItemVariantNameTooLong",
        $"The variant name must not exceed {MenuItemVariant.MaxNameLength} characters.");

    public static readonly ErrorDetail DescriptionTooLong = ErrorDetail.Validation(
        "Catalog.MenuItemVariantDescriptionTooLong",
        $"The variant description must not exceed {MenuItemVariant.MaxDescriptionLength} characters.");

    public static readonly ErrorDetail InvalidDisplayOrder = ErrorDetail.Validation(
        "Catalog.InvalidMenuItemVariantDisplayOrder",
        "The variant display order cannot be negative.");

    public static readonly ErrorDetail DefaultCannotBeDeleted = ErrorDetail.Conflict(
        "Catalog.DefaultMenuItemVariantCannotBeDeleted",
        "The default variant cannot be deleted. Select another default first.");
}
