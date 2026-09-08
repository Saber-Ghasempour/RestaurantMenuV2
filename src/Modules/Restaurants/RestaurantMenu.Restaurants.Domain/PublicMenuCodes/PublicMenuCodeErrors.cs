using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.PublicMenuCodes;

public static class PublicMenuCodeErrors
{
    public static readonly ErrorDetail InvalidPurposeScope = ErrorDetail.Validation(
        "PublicMenuCodes.InvalidPurposeScope", "Menu-only codes cannot target a table; dine-in ordering codes require both a branch and table.");
    public static readonly ErrorDetail InvalidExpiry = ErrorDetail.Validation(
        "PublicMenuCodes.InvalidExpiry", "Expiry must be later than creation or rotation time.");
    public static readonly ErrorDetail InvalidCodeHash = ErrorDetail.Validation(
        "PublicMenuCodes.InvalidCodeHash", "A code hash is required.");
    public static readonly ErrorDetail InvalidOrExpired = ErrorDetail.NotFound(
        "PublicMenuCodes.InvalidOrExpired", "The public menu code is invalid, revoked, or expired.");
    public static readonly ErrorDetail BranchInactive = ErrorDetail.Conflict(
        "PublicMenuCodes.BranchInactive", "A code cannot be issued for an inactive branch.");
    public static readonly ErrorDetail TableInactive = ErrorDetail.Conflict(
        "PublicMenuCodes.TableInactive", "A dine-in ordering code cannot be issued for an inactive table.");
    public static readonly ErrorDetail CollisionLimitExceeded = ErrorDetail.Failure(
        "PublicMenuCodes.CollisionLimitExceeded", "A unique public menu code could not be generated.");
    public static ErrorDetail NotFound(PublicMenuCodeId id) => ErrorDetail.NotFound(
        "PublicMenuCodes.NotFound", $"Public menu code with identifier '{id.Value}' was not found.");
    public static ErrorDetail VersionConflict(PublicMenuCodeId id) => ErrorDetail.Conflict(
        "PublicMenuCodes.VersionConflict", $"Public menu code with identifier '{id.Value}' was modified by another request.");
}
