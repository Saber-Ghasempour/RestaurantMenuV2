using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Restaurants;

public sealed class RestaurantReadService
    : IRestaurantReadService
{
    private readonly RestaurantsDbContext _dbContext;

    public RestaurantReadService(
        RestaurantsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<RestaurantResponse?> GetByIdAsync(
        RestaurantId restaurantId,
        CancellationToken cancellationToken)
    {
        var restaurant =
            await _dbContext.Restaurants
                .AsNoTracking()
                .Where(
                    candidate =>
                        candidate.Id == restaurantId)
                .Select(
                    candidate => new
                    {
                        candidate.Id,
                        candidate.Name,
                        candidate.CreatedAtUtc
                    })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (restaurant is null)
        {
            return null;
        }

        return new RestaurantResponse(
            restaurant.Id.Value,
            restaurant.Name,
            restaurant.CreatedAtUtc);
    }
}