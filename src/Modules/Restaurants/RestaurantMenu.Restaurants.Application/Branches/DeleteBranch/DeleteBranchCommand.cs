using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.DeleteBranch;

public sealed record DeleteBranchCommand(
    RestaurantId RestaurantId,
    BranchId BranchId,
    long ExpectedVersion) : ICommand<Result<BranchId>>;
