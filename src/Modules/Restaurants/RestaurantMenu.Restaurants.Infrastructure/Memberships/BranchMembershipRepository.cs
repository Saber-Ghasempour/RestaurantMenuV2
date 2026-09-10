using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Memberships;

public sealed class BranchMembershipRepository(RestaurantsDbContext dbContext)
    : IBranchMembershipRepository, IBranchMembershipReadService
{
    public void Add(BranchMembership membership) => dbContext.BranchMemberships.Add(membership);
    public Task<BranchMembership?> GetAsync(RestaurantId restaurantId, BranchId branchId,
        string subject, CancellationToken cancellationToken) => dbContext.BranchMemberships
        .SingleOrDefaultAsync(value => value.RestaurantId == restaurantId && value.BranchId == branchId &&
            value.Subject == subject, cancellationToken);
    public async Task<BranchMembershipAccess?> GetAccessAsync(RestaurantId restaurantId,
        BranchId branchId, string subject, CancellationToken cancellationToken) =>
        await dbContext.BranchMemberships.AsNoTracking()
            .Where(value => value.RestaurantId == restaurantId && value.BranchId == branchId &&
                value.Subject == subject && dbContext.Branches.Any(branch =>
                    branch.RestaurantId == restaurantId && branch.Id == branchId && branch.IsActive))
            .Select(value => new BranchMembershipAccess(value.Role, value.Status))
            .SingleOrDefaultAsync(cancellationToken);
}
