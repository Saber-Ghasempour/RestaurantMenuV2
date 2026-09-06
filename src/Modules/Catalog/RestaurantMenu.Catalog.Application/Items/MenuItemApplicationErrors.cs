using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items;

public static class MenuItemApplicationErrors
{
    public static ErrorDetail CategoryNotFound(
        Guid categoryId) =>
        ErrorDetail.NotFound(
            "Catalog.MenuItemCategoryNotFound",
            $"Menu category with identifier '{categoryId}' was not found for this restaurant.");

    public static ErrorDetail ItemNotFound(MenuItemId menuItemId) =>
        ErrorDetail.NotFound(
            "Catalog.MenuItemNotFound",
            $"Menu item with identifier '{menuItemId.Value}' was not found for this restaurant and category.");

    public static ErrorDetail VersionConflict(MenuItemId menuItemId) =>
        ErrorDetail.Conflict(
            "Catalog.MenuItemVersionConflict",
            $"Menu item with identifier '{menuItemId.Value}' was modified by another request. Reload it and try again.");
}
