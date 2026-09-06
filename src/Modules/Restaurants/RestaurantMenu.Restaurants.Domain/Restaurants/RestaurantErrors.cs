using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public static class RestaurantErrors
{
    public static readonly ErrorDetail NameRequired =
        ErrorDetail.Validation(
            "Restaurants.NameRequired",
            "The restaurant name is required.");

    public static readonly ErrorDetail NameTooLong =
        ErrorDetail.Validation(
            "Restaurants.NameTooLong",
            $"The restaurant name must not exceed {Restaurant.MaxNameLength} characters.");

    public static ErrorDetail NotFound(
        RestaurantId restaurantId) =>
        ErrorDetail.NotFound(
            "Restaurants.NotFound",
            $"Restaurant with identifier '{restaurantId.Value}' was not found.");

    public static ErrorDetail VersionConflict(
        RestaurantId restaurantId) =>
        ErrorDetail.Conflict(
            "Restaurants.VersionConflict",
            $"Restaurant with identifier '{restaurantId.Value}' was modified by another request. Reload it and try again.");
}
