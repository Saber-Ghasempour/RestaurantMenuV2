namespace RestaurantMenu.Restaurants.Application.Branches.GetBranch;

public sealed record BranchResponse(
    Guid Id,
    Guid RestaurantId,
    string Name,
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
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    long Version);
