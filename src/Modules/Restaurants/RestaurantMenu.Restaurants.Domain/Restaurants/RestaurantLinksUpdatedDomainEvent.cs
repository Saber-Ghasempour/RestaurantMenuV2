using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed record RestaurantLinksUpdatedDomainEvent(RestaurantId RestaurantId) : IDomainEvent;
