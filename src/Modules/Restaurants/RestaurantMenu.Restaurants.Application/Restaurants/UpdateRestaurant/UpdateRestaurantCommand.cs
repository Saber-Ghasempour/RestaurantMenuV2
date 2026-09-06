using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurant;

public sealed record UpdateRestaurantCommand(
    RestaurantId RestaurantId,
    string? Name,
    long ExpectedVersion)
    : ICommand<Result<long>>;
