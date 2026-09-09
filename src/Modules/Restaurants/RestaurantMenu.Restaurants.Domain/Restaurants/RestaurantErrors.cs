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

    public static readonly ErrorDetail InvalidDefaultCurrency =
        ErrorDetail.Validation(
            "Restaurants.InvalidDefaultCurrency",
            "The default currency must be a recognized ISO 4217 currency code.");

    public static readonly ErrorDetail InvalidDefaultLocale =
        ErrorDetail.Validation(
            "Restaurants.InvalidDefaultLocale",
            "The default locale must be a recognized culture name up to 16 characters.");

    public static readonly ErrorDetail InvalidTimeZone =
        ErrorDetail.Validation(
            "Restaurants.InvalidTimeZone",
            "The time zone must be a recognized IANA time-zone identifier up to 64 characters.");

    public static readonly ErrorDetail InvalidMediaReference =
        ErrorDetail.Validation("Restaurants.InvalidMediaReference", "Media identifiers cannot be empty.");

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
