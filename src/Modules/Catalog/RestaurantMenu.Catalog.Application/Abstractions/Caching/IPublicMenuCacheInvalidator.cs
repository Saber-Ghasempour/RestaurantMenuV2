namespace RestaurantMenu.Catalog.Application.Abstractions.Caching;

public interface IPublicMenuCacheInvalidator
{
    Task InvalidateRestaurantAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);
}
