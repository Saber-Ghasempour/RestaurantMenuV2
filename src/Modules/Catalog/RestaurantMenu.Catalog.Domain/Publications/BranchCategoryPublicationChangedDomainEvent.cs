using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Catalog.Domain.Publications;

public sealed record BranchCategoryPublicationChangedDomainEvent(
    Guid RestaurantId,
    Guid BranchId,
    MenuCategoryId CategoryId)
    : IDomainEvent;
