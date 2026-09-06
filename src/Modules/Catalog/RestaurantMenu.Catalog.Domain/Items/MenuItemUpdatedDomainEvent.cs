using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed record MenuItemUpdatedDomainEvent(
    MenuItemId MenuItemId,
    Guid RestaurantId,
    MenuCategoryId CategoryId)
    : IDomainEvent;
