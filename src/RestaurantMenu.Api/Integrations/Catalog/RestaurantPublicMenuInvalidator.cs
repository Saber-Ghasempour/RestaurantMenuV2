using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Catalog;

public sealed class RestaurantPublicMenuInvalidator(
    IPublicMenuCacheInvalidator invalidator)
    : IRestaurantPublicMenuInvalidator
{
    public Task InvalidateAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken) =>
        invalidator.InvalidateRestaurantAsync(
            restaurantId.Value, cancellationToken);
}
