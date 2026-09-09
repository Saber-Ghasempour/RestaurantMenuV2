using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Categories;

public sealed record MenuCategoryPublicationChangedDomainEvent(
    MenuCategoryId MenuCategoryId,
    Guid RestaurantId,
    bool IsPublished) : IDomainEvent;
