using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;

public static class CreateMenuCategoryErrors
{
    public static ErrorDetail RestaurantNotFound(
        Guid restaurantId) =>
        ErrorDetail.NotFound(
            "Catalog.RestaurantNotFound",
            $"Restaurant with identifier '{restaurantId}' was not found.");

    public static ErrorDetail ParentCategoryNotFound(
        Guid categoryId) =>
        ErrorDetail.NotFound(
            "Catalog.ParentCategoryNotFound",
            $"Parent category with identifier '{categoryId}' was not found for this restaurant.");
}
