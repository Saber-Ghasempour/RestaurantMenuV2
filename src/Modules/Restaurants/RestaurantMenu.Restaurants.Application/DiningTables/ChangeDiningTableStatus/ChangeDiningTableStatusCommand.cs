using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.DiningTables.ChangeDiningTableStatus;
public sealed record ChangeDiningTableStatusCommand(RestaurantId RestaurantId, BranchId BranchId,
    DiningTableId DiningTableId, bool IsActive, long ExpectedVersion) : ICommand<Result<long>>;
