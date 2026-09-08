using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Publications;

public static class BranchCategoryPublicationErrors
{
    public static readonly ErrorDetail RestaurantRequired =
        ErrorDetail.Validation(
            "Catalog.PublicationRestaurantRequired",
            "A restaurant identifier is required.");

    public static readonly ErrorDetail BranchRequired =
        ErrorDetail.Validation(
            "Catalog.PublicationBranchRequired",
            "A branch identifier is required.");

    public static readonly ErrorDetail CategoryRequired =
        ErrorDetail.Validation(
            "Catalog.PublicationCategoryRequired",
            "A category identifier is required.");

    public static readonly ErrorDetail InvalidDisplayOrder =
        ErrorDetail.Validation(
            "Catalog.PublicationInvalidDisplayOrder",
            "A display-order override cannot be negative.");
}
