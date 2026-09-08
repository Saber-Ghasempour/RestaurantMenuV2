using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Publications;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IBranchCategoryPublicationRepository
{
    Task<IReadOnlyList<PublicationCategory>> GetCategoriesAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BranchCategoryPublication>> GetByBranchAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);

    void Add(BranchCategoryPublication publication);
}

public sealed record PublicationCategory(
    MenuCategoryId Id,
    MenuCategoryId? ParentId);
