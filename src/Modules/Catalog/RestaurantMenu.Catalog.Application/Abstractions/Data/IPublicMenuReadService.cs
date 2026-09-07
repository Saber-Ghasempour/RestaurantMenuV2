using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IPublicMenuReadService
{
    Task<IReadOnlyList<PublicMenuCategoryResponse>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);
}
