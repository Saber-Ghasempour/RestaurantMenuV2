using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IDiningTableRepository
{
    void Add(DiningTable table);
    Task<DiningTable?> GetByIdAsync(RestaurantId restaurantId, BranchId branchId,
        DiningTableId diningTableId, CancellationToken cancellationToken);
}

public sealed class DiningTableNumberAlreadyExistsException(string message, Exception innerException)
    : Exception(message, innerException);
