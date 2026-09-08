using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Branches;

public static class BranchErrors
{
    public static readonly ErrorDetail NameRequired = ErrorDetail.Validation(
        "Branches.NameRequired", "The branch name is required.");

    public static readonly ErrorDetail NameTooLong = ErrorDetail.Validation(
        "Branches.NameTooLong",
        $"The branch name must not exceed {Branch.MaxNameLength} characters.");

    public static readonly ErrorDetail InvalidSlug = ErrorDetail.Validation(
        "Branches.InvalidSlug",
        "The branch slug must contain 3 to 80 ASCII letters, digits, or single hyphens.");

    public static readonly ErrorDetail DetailsTooLong = ErrorDetail.Validation(
        "Branches.DetailsTooLong", "One or more branch details exceed their maximum length.");

    public static readonly ErrorDetail InvalidCountryCode = ErrorDetail.Validation(
        "Branches.InvalidCountryCode", "Country code must contain two ASCII letters.");

    public static readonly ErrorDetail CoordinatesMustBeProvidedTogether = ErrorDetail.Validation(
        "Branches.CoordinatesMustBeProvidedTogether",
        "Latitude and longitude must either both be provided or both be omitted.");

    public static readonly ErrorDetail InvalidLatitude = ErrorDetail.Validation(
        "Branches.InvalidLatitude", "Latitude must be between -90 and 90.");

    public static readonly ErrorDetail InvalidLongitude = ErrorDetail.Validation(
        "Branches.InvalidLongitude", "Longitude must be between -180 and 180.");

    public static ErrorDetail NotFound(BranchId branchId) => ErrorDetail.NotFound(
        "Branches.NotFound", $"Branch with identifier '{branchId.Value}' was not found.");

    public static ErrorDetail VersionConflict(BranchId branchId) => ErrorDetail.Conflict(
        "Branches.VersionConflict",
        $"Branch with identifier '{branchId.Value}' was modified by another request. Reload it and try again.");

    public static readonly ErrorDetail SlugAlreadyExists = ErrorDetail.Conflict(
        "Branches.SlugAlreadyExists", "This branch slug is already in use by the restaurant.");
}
