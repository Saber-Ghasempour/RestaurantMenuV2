using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;

public sealed record CreateRestaurantCommand(
    string? Name,
    string OwnerSubject) : ICommand<Result<RestaurantId>>;
