using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;

namespace RestaurantMenu.Api.Integrations.Ordering;

public sealed class DiningTableSnapshotProvider(RestaurantsDbContext dbContext)
    : IDiningTableSnapshotProvider
{
    public async Task<string?> GetDisplayNameAsync(Guid restaurantId, Guid branchId,
        Guid diningTableId, CancellationToken cancellationToken)
    {
        var table = await dbContext.DiningTables.AsNoTracking()
            .Where(value => value.Id == new DiningTableId(diningTableId) &&
                value.RestaurantId == new RestaurantId(restaurantId) &&
                value.BranchId == new BranchId(branchId) && value.IsActive)
            .Select(value => new { value.DisplayName, value.Number })
            .SingleOrDefaultAsync(cancellationToken);
        return table is null ? null : table.DisplayName ?? table.Number.ToString(CultureInfo.InvariantCulture);
    }
}
