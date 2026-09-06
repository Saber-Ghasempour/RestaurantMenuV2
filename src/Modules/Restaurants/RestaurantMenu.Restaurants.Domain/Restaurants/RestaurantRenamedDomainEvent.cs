using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed record RestaurantRenamedDomainEvent(
    RestaurantId RestaurantId)
    : IDomainEvent;
