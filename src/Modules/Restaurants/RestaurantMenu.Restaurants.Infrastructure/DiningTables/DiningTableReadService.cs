using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.DiningTables;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.DiningTables;
public sealed class DiningTableReadService(RestaurantsDbContext dbContext) : IDiningTableReadService
{
    public async Task<IReadOnlyList<DiningTableResponse>> ListAsync(RestaurantId restaurantId,
        BranchId branchId, CancellationToken cancellationToken) =>
        await dbContext.DiningTables.AsNoTracking()
            .Where(table => table.RestaurantId == restaurantId && table.BranchId == branchId)
            .OrderBy(table => table.Number).ThenBy(table => table.Id)
            .Select(table => new DiningTableResponse(table.Id.Value, table.RestaurantId.Value,
                table.BranchId.Value, table.Number, table.DisplayName, table.Capacity,
                table.IsActive, table.CreatedAtUtc, table.Version))
            .ToArrayAsync(cancellationToken);
}
