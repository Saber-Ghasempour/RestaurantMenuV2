using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.UnitTests.TestDoubles;

internal sealed class RestaurantCacheStub(
    RestaurantResponse? cachedRestaurant = null)
    : IRestaurantCache
{
    public RestaurantResponse? CachedRestaurant { get; private set; } =
        cachedRestaurant;

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public RestaurantId? RemovedRestaurantId { get; private set; }

    public Task<RestaurantResponse?> GetAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken)
    {
        GetCallCount++;
        return Task.FromResult(CachedRestaurant);
    }

    public Task SetAsync(
        RestaurantResponse restaurant,
        CancellationToken cancellationToken)
    {
        SetCallCount++;
        CachedRestaurant = restaurant;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken)
    {
        RemoveCallCount++;
        RemovedRestaurantId = restaurantId;
        CachedRestaurant = null;
        return Task.CompletedTask;
    }
}
