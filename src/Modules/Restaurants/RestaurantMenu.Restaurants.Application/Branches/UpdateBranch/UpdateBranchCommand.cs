using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Branches.UpdateBranch;

public sealed record UpdateBranchCommand(
    RestaurantId RestaurantId,
    BranchId BranchId,
    string? Name,
    string? Slug,
    string? Phone,
    string? AddressLine,
    string? CityName,
    string? RegionName,
    string? PostalCode,
    string? CountryCode,
    decimal? Latitude,
    decimal? Longitude,
    string? TimeZoneId,
    long ExpectedVersion) : ICommand<Result<long>>;
