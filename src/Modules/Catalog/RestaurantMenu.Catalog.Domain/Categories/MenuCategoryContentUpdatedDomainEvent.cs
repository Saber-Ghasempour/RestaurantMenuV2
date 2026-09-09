using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Categories;

public sealed record MenuCategoryContentUpdatedDomainEvent(
    MenuCategoryId MenuCategoryId,
    Guid RestaurantId) : IDomainEvent;
