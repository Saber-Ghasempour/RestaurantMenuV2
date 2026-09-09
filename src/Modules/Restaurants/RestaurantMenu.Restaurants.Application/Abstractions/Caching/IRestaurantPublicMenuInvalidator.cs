using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Caching;

public interface IRestaurantPublicMenuInvalidator
{
    Task InvalidateAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken);
}
