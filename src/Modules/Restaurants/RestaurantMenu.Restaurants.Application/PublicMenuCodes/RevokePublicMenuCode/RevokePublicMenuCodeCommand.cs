using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.RevokePublicMenuCode;
public sealed record RevokePublicMenuCodeCommand(RestaurantId RestaurantId,
    PublicMenuCodeId PublicMenuCodeId, long ExpectedVersion) : ICommand<Result<long>>;
