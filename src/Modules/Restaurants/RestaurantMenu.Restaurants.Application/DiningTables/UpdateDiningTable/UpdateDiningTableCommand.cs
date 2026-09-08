using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.UpdateDiningTable;
public sealed record UpdateDiningTableCommand(RestaurantId RestaurantId, BranchId BranchId,
    DiningTableId DiningTableId, int Number, string? DisplayName, short? Capacity,
    long ExpectedVersion) : ICommand<Result<long>>;
