using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed record MenuItemAvailabilityChangedDomainEvent(
    MenuItemId MenuItemId,
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    bool IsAvailable)
    : IDomainEvent;
