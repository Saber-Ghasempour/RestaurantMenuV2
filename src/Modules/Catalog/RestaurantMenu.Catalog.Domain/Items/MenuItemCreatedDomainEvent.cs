using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed record MenuItemCreatedDomainEvent(
    MenuItemId MenuItemId,
    Guid RestaurantId)
    : IDomainEvent;
