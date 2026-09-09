using RestaurantMenu.Catalog.Application.Abstractions.Caching;

namespace RestaurantMenu.Catalog.Application.UnitTests;

internal sealed class PublicMenuCacheInvalidatorStub : IPublicMenuCacheInvalidator
{
    public Task InvalidateRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
