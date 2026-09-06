using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories;

public static class MenuCategoryApplicationErrors
{
    public static ErrorDetail RestaurantNotFound(
        Guid restaurantId) =>
        ErrorDetail.NotFound(
            "Catalog.RestaurantNotFound",
            $"Restaurant with identifier '{restaurantId}' was not found.");

    public static ErrorDetail CategoryNotFound(
        MenuCategoryId categoryId) =>
        ErrorDetail.NotFound(
            "Catalog.CategoryNotFound",
            $"Menu category with identifier '{categoryId.Value}' was not found for this restaurant.");

    public static ErrorDetail ParentCategoryNotFound(
        Guid categoryId) =>
        ErrorDetail.NotFound(
            "Catalog.ParentCategoryNotFound",
            $"Parent category with identifier '{categoryId}' was not found for this restaurant.");

    public static ErrorDetail VersionConflict(
        MenuCategoryId categoryId) =>
        ErrorDetail.Conflict(
            "Catalog.CategoryVersionConflict",
            $"Menu category with identifier '{categoryId.Value}' was modified by another request. Reload it and try again.");

    public static ErrorDetail HierarchyCycle(
        MenuCategoryId categoryId) =>
        ErrorDetail.Validation(
            "Catalog.CategoryHierarchyCycle",
            $"Menu category with identifier '{categoryId.Value}' cannot be moved below itself or one of its descendants.");

    public static ErrorDetail HasChildren(
        MenuCategoryId categoryId) =>
        ErrorDetail.Conflict(
            "Catalog.CategoryHasChildren",
            $"Menu category with identifier '{categoryId.Value}' cannot be deleted while it has active child categories.");
}
