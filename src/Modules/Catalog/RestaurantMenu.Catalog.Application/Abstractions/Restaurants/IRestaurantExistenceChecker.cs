namespace RestaurantMenu.Catalog.Application.Abstractions.Restaurants;

public interface IRestaurantExistenceChecker
{
    Task<bool> ExistsAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);
}
