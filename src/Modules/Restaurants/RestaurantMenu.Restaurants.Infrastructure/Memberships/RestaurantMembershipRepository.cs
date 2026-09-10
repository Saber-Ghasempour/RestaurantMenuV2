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
    public Task<RestaurantMembership?> GetAsync(RestaurantId restaurantId, string subject, CancellationToken cancellationToken) => dbContext.RestaurantMemberships.SingleOrDefaultAsync(x => x.RestaurantId == restaurantId && x.Subject == subject, cancellationToken);
    public Task<int> CountActiveOwnersAsync(RestaurantId restaurantId, CancellationToken cancellationToken) => dbContext.RestaurantMemberships.CountAsync(x => x.RestaurantId == restaurantId && x.Role == RestaurantMembershipRole.Owner && x.Status == RestaurantMembershipStatus.Active, cancellationToken);
    public async Task<IReadOnlyList<RestaurantMenu.Restaurants.Application.Memberships.MemberResponse>> ListAsync(RestaurantId restaurantId, CancellationToken cancellationToken)
    { var values = await dbContext.RestaurantMemberships.AsNoTracking().Where(x => x.RestaurantId == restaurantId).OrderBy(x => x.Subject).ToArrayAsync(cancellationToken); return values.Select(x => new RestaurantMenu.Restaurants.Application.Memberships.MemberResponse(x.Subject, x.Role.ToString(), x.Status.ToString(), x.Version)).ToArray(); }

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
                    membership.Subject == subject && membership.Status == RestaurantMembershipStatus.Active,
                cancellationToken);
    }
}