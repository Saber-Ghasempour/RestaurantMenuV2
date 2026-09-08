using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IBranchRepository
{
    void Add(Branch branch);

    Task<Branch?> GetByIdAsync(
        RestaurantId restaurantId,
        BranchId branchId,
        CancellationToken cancellationToken);
}
