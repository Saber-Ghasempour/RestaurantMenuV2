using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Items.CreateMenuItem;

public static class CreateMenuItemApplicationErrors
{
    public static ErrorDetail CategoryNotFound(
        Guid categoryId) =>
        ErrorDetail.NotFound(
            "Catalog.MenuItemCategoryNotFound",
            $"Menu category with identifier '{categoryId}' was not found for this restaurant.");
}
