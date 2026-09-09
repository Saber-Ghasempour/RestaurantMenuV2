namespace RestaurantMenu.Restaurants.Application.Abstractions.Media;

public interface IMediaAssetValidator
{
    Task<bool> IsReadyAsync(Guid restaurantId, Guid mediaAssetId, CancellationToken cancellationToken);
}
