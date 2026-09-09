using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed record RestaurantDefaultsUpdatedDomainEvent(RestaurantId RestaurantId)
    : IDomainEvent;
