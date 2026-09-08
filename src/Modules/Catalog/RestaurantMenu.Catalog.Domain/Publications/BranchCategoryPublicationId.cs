using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Domain.Publications;

public readonly record struct BranchCategoryPublicationId(
    Guid BranchId,
    MenuCategoryId CategoryId);
