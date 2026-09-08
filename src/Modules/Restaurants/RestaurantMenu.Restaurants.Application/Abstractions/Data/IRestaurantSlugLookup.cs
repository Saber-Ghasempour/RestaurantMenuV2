namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IRestaurantSlugLookup
{
    Task<Guid?> FindRestaurantIdAsync(string slug, CancellationToken cancellationToken);
}
