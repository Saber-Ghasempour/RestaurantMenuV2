using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Restaurants.Application.Restaurants.ChangeRestaurantSlug;
public sealed record ChangeRestaurantSlugCommand(RestaurantId RestaurantId, string? Slug, long ExpectedVersion)
    : ICommand<Result<long>>;
