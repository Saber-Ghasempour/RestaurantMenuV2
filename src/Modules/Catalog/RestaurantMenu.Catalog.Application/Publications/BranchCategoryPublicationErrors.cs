using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Publications;

public static class BranchCategoryPublicationErrors
{
    public static ErrorDetail BranchNotFound(Guid branchId) =>
        ErrorDetail.NotFound(
            "Catalog.BranchNotFound",
            $"Branch with identifier '{branchId}' was not found for this restaurant.");

    public static readonly ErrorDetail CompleteSetRequired =
        ErrorDetail.Validation(
            "Catalog.PublicationCompleteSetRequired",
            "The request must contain every active category exactly once and no category from another restaurant.");

    public static ErrorDetail PublishedParentRequired(Guid categoryId) =>
        ErrorDetail.Validation(
            "Catalog.PublishedParentRequired",
            $"Published category '{categoryId}' requires its parent category to be published.");

    public static readonly ErrorDetail VersionConflict =
        ErrorDetail.Conflict(
            "Catalog.PublicationVersionConflict",
            "Branch category publication was modified by another request. Reload it and try again.");
}
