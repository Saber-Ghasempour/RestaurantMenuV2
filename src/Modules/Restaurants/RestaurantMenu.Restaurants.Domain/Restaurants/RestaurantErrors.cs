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
}