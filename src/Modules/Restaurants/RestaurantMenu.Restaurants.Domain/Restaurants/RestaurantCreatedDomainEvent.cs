using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.Restaurants;

public sealed record RestaurantCreatedDomainEvent(RestaurantId RestaurantId) : IDomainEvent;