using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.SetRestaurantBranding;

public sealed record SetRestaurantBrandingCommand(RestaurantId RestaurantId, Guid? LogoMediaId,
    Guid? CoverMediaId, long ExpectedVersion) : ICommand<Result<long>>;
