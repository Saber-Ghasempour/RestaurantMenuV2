using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;

public static class ListRestaurantsErrors
{
    public static readonly ErrorDetail InvalidPage =
        ErrorDetail.Validation(
            "Restaurants.InvalidPage",
            "Page must be greater than zero.");

    public static readonly ErrorDetail InvalidPageSize =
        ErrorDetail.Validation(
            "Restaurants.InvalidPageSize",
            $"Page size must be between 1 and {ListRestaurantsQuery.MaxPageSize}.");
}
