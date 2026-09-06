using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Restaurants;

public sealed class RestaurantRepository
    : IRestaurantRepository
{
    private readonly RestaurantsDbContext _dbContext;

    public RestaurantRepository(
        RestaurantsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public void Add(Restaurant restaurant)
    {
        ArgumentNullException.ThrowIfNull(restaurant);

        _dbContext.Restaurants.Add(restaurant);
    }
}