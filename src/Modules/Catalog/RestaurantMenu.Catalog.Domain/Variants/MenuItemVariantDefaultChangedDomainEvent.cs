using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Variants;

public sealed record MenuItemVariantDefaultChangedDomainEvent(
    MenuItemVariantId VariantId,
    Guid RestaurantId,
    MenuItemId MenuItemId,
    bool IsDefault) : IDomainEvent;
