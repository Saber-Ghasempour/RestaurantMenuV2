using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

namespace RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;

public sealed record RestaurantsPage(
    IReadOnlyList<RestaurantResponse> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                (double)TotalCount / PageSize);
}
