using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Categories;

public sealed class MenuCategoryReadService
    : IMenuCategoryReadService
{
    private readonly CatalogDbContext _dbContext;

    public MenuCategoryReadService(CatalogDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<MenuCategoryResponse?> GetByIdAsync(
        Guid restaurantId,
        MenuCategoryId categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.MenuCategories
            .AsNoTracking()
            .Where(category =>
                category.RestaurantId == restaurantId &&
                category.Id == categoryId)
            .Select(category => new MenuCategoryResponse(
                category.Id.Value,
                category.RestaurantId,
                category.ParentId.HasValue
                    ? category.ParentId.Value.Value
                    : null,
                category.Name,
                category.DisplayOrder,
                category.CreatedAtUtc,
                category.Version,
                category.Description,
                category.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuCategoryResponse>>
        GetByRestaurantIdAsync(
            Guid restaurantId,
            CancellationToken cancellationToken)
    {
        return await _dbContext.MenuCategories
            .AsNoTracking()
            .Where(category =>
                category.RestaurantId == restaurantId)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new MenuCategoryResponse(
                category.Id.Value,
                category.RestaurantId,
                category.ParentId.HasValue
                    ? category.ParentId.Value.Value
                    : null,
                category.Name,
                category.DisplayOrder,
                category.CreatedAtUtc,
                category.Version,
                category.Description,
                category.IsPublished))
            .ToArrayAsync(cancellationToken);
    }
}
