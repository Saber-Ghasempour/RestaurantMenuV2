using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.CreateDiningTable;

public sealed record CreateDiningTableCommand(RestaurantId RestaurantId, BranchId BranchId,
    int Number, string? DisplayName, short? Capacity) : ICommand<Result<DiningTableId>>;
