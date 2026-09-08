using RestaurantMenu.Restaurants.Application.DiningTables;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IDiningTableReadService
{
    Task<IReadOnlyList<DiningTableResponse>> ListAsync(RestaurantId restaurantId,
        BranchId branchId, CancellationToken cancellationToken);
}
