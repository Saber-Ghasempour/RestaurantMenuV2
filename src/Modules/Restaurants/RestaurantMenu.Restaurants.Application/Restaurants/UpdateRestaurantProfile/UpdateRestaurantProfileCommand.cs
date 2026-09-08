using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantProfile;
public sealed record UpdateRestaurantProfileCommand(
    RestaurantId RestaurantId,
    string? Description,
    string? About,
    string? Address,
    long ExpectedVersion) : ICommand<Result<long>>;
