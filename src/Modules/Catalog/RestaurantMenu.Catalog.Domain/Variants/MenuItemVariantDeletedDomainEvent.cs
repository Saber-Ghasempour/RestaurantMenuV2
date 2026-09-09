using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Variants;

public sealed record MenuItemVariantDeletedDomainEvent(
    MenuItemVariantId VariantId,
    Guid RestaurantId,
    MenuItemId MenuItemId,
    DateTimeOffset DeletedAtUtc) : IDomainEvent;
