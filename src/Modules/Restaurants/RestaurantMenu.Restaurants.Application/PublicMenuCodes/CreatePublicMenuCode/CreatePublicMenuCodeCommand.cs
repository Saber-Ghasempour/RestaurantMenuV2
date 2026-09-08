using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.CreatePublicMenuCode;
public sealed record CreatePublicMenuCodeCommand(RestaurantId RestaurantId, BranchId? BranchId,
    DiningTableId? DiningTableId, PublicMenuCodePurpose Purpose, DateTimeOffset? ExpiresAtUtc)
    : ICommand<Result<IssuedPublicMenuCode>>;
