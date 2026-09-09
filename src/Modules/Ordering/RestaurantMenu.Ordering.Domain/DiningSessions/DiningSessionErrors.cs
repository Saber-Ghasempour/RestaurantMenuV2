using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Domain.DiningSessions;

public static class DiningSessionErrors
{
    public static readonly ErrorDetail InvalidTokenHash = ErrorDetail.Validation(
        "DiningSession.InvalidTokenHash", "The dining-session token hash is invalid.");
    public static readonly ErrorDetail InvalidScope = ErrorDetail.Validation(
        "DiningSession.InvalidScope", "A dining session requires a Restaurant, Branch, and dining table.");
    public static readonly ErrorDetail InvalidExpiry = ErrorDetail.Validation(
        "DiningSession.InvalidExpiry", "The dining-session expiry must be after its creation time.");
    public static readonly ErrorDetail InvalidPublicCode = ErrorDetail.NotFound(
        "DiningSession.InvalidPublicCode", "The public menu code cannot start a dining session.");
    public static readonly ErrorDetail TokenCollisionLimitExceeded = ErrorDetail.Conflict(
        "DiningSession.TokenCollisionLimitExceeded", "A unique dining-session token could not be issued.");
    public static readonly ErrorDetail InvalidCapability = new(
        "DiningSession.InvalidCapability", "The dining-session capability is missing or invalid.", ErrorType.Unauthorized);
}
