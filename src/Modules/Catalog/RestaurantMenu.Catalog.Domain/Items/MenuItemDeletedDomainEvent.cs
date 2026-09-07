using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed record MenuItemDeletedDomainEvent(
    MenuItemId MenuItemId,
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    DateTimeOffset DeletedAtUtc)
    : IDomainEvent;
