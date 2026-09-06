using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Categories;

public sealed record MenuCategoryUpdatedDomainEvent(
    MenuCategoryId MenuCategoryId,
    Guid RestaurantId)
    : IDomainEvent;
