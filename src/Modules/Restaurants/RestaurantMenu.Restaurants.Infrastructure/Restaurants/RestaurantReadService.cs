using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
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

    public async Task<RestaurantsPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query =
            _dbContext.Restaurants
                .AsNoTracking();

        var totalCount =
            await query.CountAsync(cancellationToken);

        var items =
            await query
                .OrderByDescending(restaurant =>
                    restaurant.CreatedAtUtc)
                .ThenBy(restaurant => restaurant.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(restaurant =>
                    new RestaurantResponse(
                        restaurant.Id.Value,
                        restaurant.Name,
                        restaurant.CreatedAtUtc))
                .ToArrayAsync(cancellationToken);

        return new RestaurantsPage(
            items,
            page,
            pageSize,
            totalCount);
    }
}
