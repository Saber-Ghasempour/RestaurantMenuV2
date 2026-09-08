using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Restaurants;

public sealed class RestaurantSlugLookup(RestaurantsDbContext dbContext) : IRestaurantSlugLookup
{
    public async Task<Guid?> FindRestaurantIdAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalized = slug.Trim().ToLowerInvariant();
        return await dbContext.Restaurants.AsNoTracking()
            .Where(restaurant => restaurant.Slug == normalized)
            .Select(restaurant => (Guid?)restaurant.Id.Value)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
