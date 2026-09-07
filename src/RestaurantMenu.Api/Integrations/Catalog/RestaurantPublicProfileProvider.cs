using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Catalog;

public sealed class RestaurantPublicProfileProvider(
    IRestaurantReadService restaurantReadService)
    : IRestaurantPublicProfileProvider
{
    public async Task<RestaurantPublicProfile?> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var restaurant = await restaurantReadService.GetByIdAsync(
            new RestaurantId(restaurantId),
            cancellationToken);

        return restaurant is null
            ? null
            : new RestaurantPublicProfile(
                restaurant.Id,
                restaurant.Name);
    }
}
