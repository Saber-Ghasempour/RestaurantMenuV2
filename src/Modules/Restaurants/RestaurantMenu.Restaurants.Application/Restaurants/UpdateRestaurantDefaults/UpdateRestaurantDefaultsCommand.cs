using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantDefaults;

public sealed record UpdateRestaurantDefaultsCommand(
    RestaurantId RestaurantId,
    string? DefaultCurrency,
    string? DefaultLocale,
    string? TimeZoneId,
    long ExpectedVersion) : ICommand<Result<long>>;
