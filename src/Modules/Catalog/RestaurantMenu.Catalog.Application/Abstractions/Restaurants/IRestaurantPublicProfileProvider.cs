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
    string? Address = null,
    string? WebsiteUrl = null,
    string? InstagramUrl = null,
    string? FacebookUrl = null,
    string? WhatsAppUrl = null,
    string? TelegramUrl = null,
    string? TwitterUrl = null,
    string? Slug = null);
