using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Categories;

public sealed record MenuCategoryCreatedDomainEvent(
    MenuCategoryId MenuCategoryId,
    Guid RestaurantId)
    : IDomainEvent;
