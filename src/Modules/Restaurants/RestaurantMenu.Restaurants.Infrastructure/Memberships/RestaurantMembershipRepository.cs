using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Memberships;

public sealed class RestaurantMembershipRepository(
    RestaurantsDbContext dbContext)
    : IRestaurantMembershipRepository,
      IRestaurantMembershipReadService
{
    public void Add(RestaurantMembership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        dbContext.RestaurantMemberships.Add(membership);
    }

    public Task<bool> HasAccessAsync(
        RestaurantId restaurantId,
        string subject,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return dbContext.RestaurantMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.RestaurantId == restaurantId &&
                    membership.Subject == subject,
                cancellationToken);
    }
}
