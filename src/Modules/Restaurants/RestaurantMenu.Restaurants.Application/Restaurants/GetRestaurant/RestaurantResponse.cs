using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;

public sealed record RestaurantResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    long Version,
    string? Description = null,
    string? About = null,
    string? Address = null,
    string? WebsiteUrl = null,
    string? InstagramUrl = null,
    string? FacebookUrl = null,
    string? WhatsAppUrl = null,
    string? TelegramUrl = null,
    string? TwitterUrl = null,
    string? Slug = null,
    string DefaultCurrency = Restaurant.InitialDefaultCurrency,
    string DefaultLocale = Restaurant.InitialDefaultLocale,
    string TimeZoneId = Restaurant.InitialTimeZoneId);
