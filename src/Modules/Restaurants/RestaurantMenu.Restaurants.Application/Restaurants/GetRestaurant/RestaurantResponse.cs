namespace RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

public sealed record RestaurantResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    long Version,
    string? Description = null,
    string? About = null,
    string? Address = null);
