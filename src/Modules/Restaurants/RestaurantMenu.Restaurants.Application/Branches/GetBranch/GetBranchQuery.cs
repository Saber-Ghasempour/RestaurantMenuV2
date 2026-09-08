using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.GetBranch;

public sealed record GetBranchQuery(
    RestaurantId RestaurantId,
    BranchId BranchId) : IQuery<Result<BranchResponse>>;
