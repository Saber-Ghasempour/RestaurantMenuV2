using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuItemVariantReadService
{
    Task<MenuItemVariantResponse?> GetByIdAsync(
        Guid restaurantId,
        MenuItemId menuItemId,
        MenuItemVariantId variantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuItemVariantResponse>> GetByMenuItemIdAsync(
        Guid restaurantId,
        MenuItemId menuItemId,
        CancellationToken cancellationToken);
}
