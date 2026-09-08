using RestaurantMenu.Catalog.Application.Publications;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IBranchCatalogReadService
{
    Task<IReadOnlyList<BranchCategoryPublicationResponse>> GetConfigurationAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PublicMenuCategoryResponse>> GetPublicMenuAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);
}
