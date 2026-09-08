using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Catalog;

public sealed class BranchExistenceChecker(IBranchReadService readService)
    : IBranchExistenceChecker
{
    public async Task<bool> ExistsAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken) =>
        await readService.GetByIdAsync(
            new RestaurantId(restaurantId),
            new BranchId(branchId),
            cancellationToken) is not null;
}
