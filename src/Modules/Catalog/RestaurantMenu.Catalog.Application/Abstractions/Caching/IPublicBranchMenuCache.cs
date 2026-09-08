using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;

namespace RestaurantMenu.Catalog.Application.Abstractions.Caching;

public interface IPublicBranchMenuCache
{
    Task<PublicBranchMenuResponse?> GetAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);

    Task SetAsync(
        PublicBranchMenuResponse menu,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken);
}
