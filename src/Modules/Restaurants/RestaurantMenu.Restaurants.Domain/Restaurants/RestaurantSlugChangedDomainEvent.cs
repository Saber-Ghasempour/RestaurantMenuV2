using RestaurantMenu.SharedKernel.Domain;
namespace RestaurantMenu.Restaurants.Domain.Restaurants;
public sealed record RestaurantSlugChangedDomainEvent(RestaurantId RestaurantId) : IDomainEvent;
