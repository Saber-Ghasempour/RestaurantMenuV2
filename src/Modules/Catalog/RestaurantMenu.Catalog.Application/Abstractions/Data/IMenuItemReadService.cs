using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuItemReadService
{
    Task<MenuItemResponse?> GetByIdAsync(
        Guid restaurantId,
        MenuCategoryId categoryId,
        MenuItemId menuItemId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuItemResponse>> GetByCategoryIdAsync(
        Guid restaurantId,
        MenuCategoryId categoryId,
        CancellationToken cancellationToken);
}
