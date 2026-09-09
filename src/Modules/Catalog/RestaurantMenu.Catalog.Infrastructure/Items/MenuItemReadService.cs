using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Items;

public sealed class MenuItemReadService : IMenuItemReadService
{
    private readonly CatalogDbContext _dbContext;

    public MenuItemReadService(CatalogDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<MenuItemResponse?> GetByIdAsync(
        Guid restaurantId,
        MenuCategoryId categoryId,
        MenuItemId menuItemId,
        CancellationToken cancellationToken)
    {
        return _dbContext.MenuItems
            .AsNoTracking()
            .Where(menuItem =>
                menuItem.RestaurantId == restaurantId &&
                menuItem.CategoryId == categoryId &&
                menuItem.Id == menuItemId)
            .Select(menuItem => new MenuItemResponse(
                menuItem.Id.Value,
                menuItem.RestaurantId,
                menuItem.CategoryId.Value,
                menuItem.Name,
                menuItem.Description,
                menuItem.Price.Amount,
                menuItem.Price.Currency,
                menuItem.DisplayOrder,
                menuItem.IsAvailable,
                menuItem.CreatedAtUtc,
                menuItem.Version,
                menuItem.Recipe,
                menuItem.Calories,
                menuItem.Tags,
                menuItem.AllergenNotes,
                menuItem.PreparationTimeMinutes,
                menuItem.IsFeatured,
                menuItem.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItemResponse>>
        GetByCategoryIdAsync(
            Guid restaurantId,
            MenuCategoryId categoryId,
            CancellationToken cancellationToken)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Where(menuItem =>
                menuItem.RestaurantId == restaurantId &&
                menuItem.CategoryId == categoryId)
            .OrderBy(menuItem => menuItem.DisplayOrder)
            .ThenBy(menuItem => menuItem.Name)
            .ThenBy(menuItem => menuItem.Id)
            .Select(menuItem => new MenuItemResponse(
                menuItem.Id.Value,
                menuItem.RestaurantId,
                menuItem.CategoryId.Value,
                menuItem.Name,
                menuItem.Description,
                menuItem.Price.Amount,
                menuItem.Price.Currency,
                menuItem.DisplayOrder,
                menuItem.IsAvailable,
                menuItem.CreatedAtUtc,
                menuItem.Version,
                menuItem.Recipe,
                menuItem.Calories,
                menuItem.Tags,
                menuItem.AllergenNotes,
                menuItem.PreparationTimeMinutes,
                menuItem.IsFeatured,
                menuItem.IsPublished))
            .ToArrayAsync(cancellationToken);
    }
}
