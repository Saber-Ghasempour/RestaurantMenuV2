using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes.RotatePublicMenuCode;
public sealed record RotatePublicMenuCodeCommand(RestaurantId RestaurantId, PublicMenuCodeId PublicMenuCodeId,
    DateTimeOffset? ExpiresAtUtc, long ExpectedVersion) : ICommand<Result<IssuedPublicMenuCode>>;
