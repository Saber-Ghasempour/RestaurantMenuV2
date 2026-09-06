using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuCategoryReadService
{
    Task<MenuCategoryResponse?> GetByIdAsync(
        Guid restaurantId,
        MenuCategoryId categoryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuCategoryResponse>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken);
}
