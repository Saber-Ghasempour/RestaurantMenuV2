using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Categories;

public sealed record MenuCategoryDeletedDomainEvent(
    MenuCategoryId MenuCategoryId,
    Guid RestaurantId,
    DateTimeOffset DeletedAtUtc)
    : IDomainEvent;
