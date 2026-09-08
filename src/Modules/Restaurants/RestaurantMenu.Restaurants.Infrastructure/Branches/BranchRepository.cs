using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.Branches;

public sealed class BranchRepository(RestaurantsDbContext dbContext)
    : IBranchRepository
{
    public void Add(Branch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        dbContext.Branches.Add(branch);
    }

    public Task<Branch?> GetByIdAsync(
        RestaurantId restaurantId,
        BranchId branchId,
        CancellationToken cancellationToken) =>
        dbContext.Branches.SingleOrDefaultAsync(
            branch => branch.RestaurantId == restaurantId && branch.Id == branchId,
            cancellationToken);
}
