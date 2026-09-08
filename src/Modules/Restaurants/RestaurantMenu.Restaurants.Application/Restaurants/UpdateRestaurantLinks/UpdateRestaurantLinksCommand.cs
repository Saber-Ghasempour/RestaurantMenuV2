using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantLinks;

public sealed record UpdateRestaurantLinksCommand(
    RestaurantId RestaurantId,
    string? WebsiteUrl,
    string? InstagramUrl,
    string? FacebookUrl,
    string? WhatsAppUrl,
    string? TelegramUrl,
    string? TwitterUrl,
    long ExpectedVersion) : ICommand<Result<long>>;
