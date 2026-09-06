using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

public sealed record GetRestaurantQuery(
    RestaurantId RestaurantId)
    : IQuery<Result<RestaurantResponse>>;