using RestaurantMenu.SharedKernel.Domain;
namespace RestaurantMenu.Restaurants.Domain.Restaurants;
public sealed record RestaurantProfileUpdatedDomainEvent(RestaurantId RestaurantId) : IDomainEvent;
