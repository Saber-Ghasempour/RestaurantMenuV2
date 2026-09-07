using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Caching;

public interface IRestaurantCache
{
    Task<RestaurantResponse?> GetAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken);

    Task SetAsync(
        RestaurantResponse restaurant,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken);
}
