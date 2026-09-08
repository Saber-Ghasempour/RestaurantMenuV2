using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Restaurants.Infrastructure.DiningTables;
public sealed class DiningTableRepository(RestaurantsDbContext dbContext) : IDiningTableRepository
{
    public void Add(DiningTable table) => dbContext.DiningTables.Add(table);
    public Task<DiningTable?> GetByIdAsync(RestaurantId restaurantId, BranchId branchId,
        DiningTableId diningTableId, CancellationToken cancellationToken) =>
        dbContext.DiningTables.SingleOrDefaultAsync(table => table.RestaurantId == restaurantId &&
            table.BranchId == branchId && table.Id == diningTableId, cancellationToken);
}
