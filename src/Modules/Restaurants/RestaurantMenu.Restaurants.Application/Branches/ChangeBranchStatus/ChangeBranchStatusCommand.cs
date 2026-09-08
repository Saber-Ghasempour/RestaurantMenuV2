using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.ChangeBranchStatus;

public sealed record ChangeBranchStatusCommand(
    RestaurantId RestaurantId,
    BranchId BranchId,
    bool IsActive,
    long ExpectedVersion) : ICommand<Result<long>>;
