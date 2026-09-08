using RestaurantMenu.Restaurants.Application.Branches.GetBranch;
using RestaurantMenu.Restaurants.Application.Branches.ListBranches;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IBranchReadService
{
    Task<BranchResponse?> GetByIdAsync(
        RestaurantId restaurantId,
        BranchId branchId,
        CancellationToken cancellationToken);

    Task<BranchesPage> GetPageAsync(
        RestaurantId restaurantId,
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);
}
