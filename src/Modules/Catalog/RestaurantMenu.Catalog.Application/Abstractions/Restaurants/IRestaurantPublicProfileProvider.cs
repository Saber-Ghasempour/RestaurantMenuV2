namespace RestaurantMenu.Catalog.Application.Abstractions.Restaurants;

public interface IRestaurantPublicProfileProvider
{
    Task<RestaurantPublicProfile?> GetAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);
}

public sealed record RestaurantPublicProfile(
    Guid Id,
    string Name,
    string? Description = null,
    string? About = null,
    string? Address = null);
