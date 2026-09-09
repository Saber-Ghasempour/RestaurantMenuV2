using RestaurantMenu.Catalog.Application.Abstractions.Caching;

namespace RestaurantMenu.Catalog.Application.UnitTests;

internal sealed class PublicMenuCacheInvalidatorStub : IPublicMenuCacheInvalidator
{
    public int InvalidationCount { get; private set; }

    public Task InvalidateRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        InvalidationCount++;
        return Task.CompletedTask;
    }
}
