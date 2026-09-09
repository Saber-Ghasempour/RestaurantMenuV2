using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuCategoryRepository
{
    void Add(MenuCategory category);

    Task<MenuCategory?> GetByIdAsync(
        MenuCategoryId categoryId,
        CancellationToken cancellationToken);

    Task<bool> HasChildrenAsync(
        MenuCategoryId categoryId,
        CancellationToken cancellationToken);

    Task<bool> HasPublishedChildrenAsync(
        MenuCategoryId categoryId,
        CancellationToken cancellationToken);
}
