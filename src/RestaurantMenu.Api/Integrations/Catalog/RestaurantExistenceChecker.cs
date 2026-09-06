using RestaurantMenu.Catalog.Application.Abstractions.Restaurants;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Catalog;

public sealed class RestaurantExistenceChecker
    : IRestaurantExistenceChecker
{
    private readonly IRestaurantReadService _readService;

    public RestaurantExistenceChecker(
        IRestaurantReadService readService)
    {
        ArgumentNullException.ThrowIfNull(readService);
        _readService = readService;
    }

    public async Task<bool> ExistsAsync(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var restaurant = await _readService.GetByIdAsync(
            new RestaurantId(restaurantId),
            cancellationToken);

        return restaurant is not null;
    }
}
