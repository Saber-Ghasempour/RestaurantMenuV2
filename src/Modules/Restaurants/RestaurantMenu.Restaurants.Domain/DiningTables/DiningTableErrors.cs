using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.DiningTables;

public static class DiningTableErrors
{
    public static readonly ErrorDetail InvalidNumber = ErrorDetail.Validation(
        "DiningTables.InvalidNumber", "The table number must be positive.");
    public static readonly ErrorDetail DisplayNameTooLong = ErrorDetail.Validation(
        "DiningTables.DisplayNameTooLong", $"The display name must not exceed {DiningTable.MaxDisplayNameLength} characters.");
    public static readonly ErrorDetail InvalidCapacity = ErrorDetail.Validation(
        "DiningTables.InvalidCapacity", "Capacity must be positive when supplied.");
    public static readonly ErrorDetail NumberAlreadyExists = ErrorDetail.Conflict(
        "DiningTables.NumberAlreadyExists", "This table number is already in use by the branch.");
    public static readonly ErrorDetail BranchInactive = ErrorDetail.Conflict(
        "DiningTables.BranchInactive", "Dining tables cannot be created or activated in an inactive branch.");
    public static ErrorDetail NotFound(DiningTableId id) => ErrorDetail.NotFound(
        "DiningTables.NotFound", $"Dining table with identifier '{id.Value}' was not found.");
    public static ErrorDetail VersionConflict(DiningTableId id) => ErrorDetail.Conflict(
        "DiningTables.VersionConflict", $"Dining table with identifier '{id.Value}' was modified by another request.");
}
