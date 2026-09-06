using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IRestaurantReadService
{
    Task<RestaurantResponse?> GetByIdAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken);

    Task<RestaurantsPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
