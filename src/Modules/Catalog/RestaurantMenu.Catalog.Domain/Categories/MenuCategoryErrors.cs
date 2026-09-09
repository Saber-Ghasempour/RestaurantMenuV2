using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Categories;

public static class MenuCategoryErrors
{
    public static readonly ErrorDetail DescriptionTooLong =
        ErrorDetail.Validation(
            "Catalog.CategoryDescriptionTooLong",
            $"The category description must not exceed {MenuCategory.MaxDescriptionLength} characters.");

    public static readonly ErrorDetail RestaurantRequired =
        ErrorDetail.Validation(
            "Catalog.RestaurantRequired",
            "A restaurant identifier is required.");

    public static readonly ErrorDetail NameRequired =
        ErrorDetail.Validation(
            "Catalog.CategoryNameRequired",
            "The category name is required.");

    public static readonly ErrorDetail NameTooLong =
        ErrorDetail.Validation(
            "Catalog.CategoryNameTooLong",
            $"The category name must not exceed {MenuCategory.MaxNameLength} characters.");

    public static readonly ErrorDetail InvalidDisplayOrder =
        ErrorDetail.Validation(
            "Catalog.InvalidCategoryDisplayOrder",
            "The category display order cannot be negative.");

    public static readonly ErrorDetail CannotBeOwnParent =
        ErrorDetail.Validation(
            "Catalog.CategoryCannotBeOwnParent",
            "A menu category cannot be its own parent.");
}
