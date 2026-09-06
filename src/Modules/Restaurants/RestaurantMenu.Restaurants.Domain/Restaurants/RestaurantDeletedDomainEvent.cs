using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed record RestaurantDeletedDomainEvent(
    RestaurantId RestaurantId,
    DateTimeOffset DeletedAtUtc)
    : IDomainEvent;
