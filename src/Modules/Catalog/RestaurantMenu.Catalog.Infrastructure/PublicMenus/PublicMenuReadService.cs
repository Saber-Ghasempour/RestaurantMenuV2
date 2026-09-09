using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.PublicMenus;

public sealed class PublicMenuReadService(CatalogDbContext dbContext)
    : IPublicMenuReadService
{
    public async Task<IReadOnlyList<PublicMenuCategoryResponse>>
        GetByRestaurantIdAsync(
            Guid restaurantId,
            CancellationToken cancellationToken)
    {
        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .Where(category =>
                category.RestaurantId == restaurantId &&
                category.IsPublished)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new
            {
                Id = category.Id.Value,
                ParentCategoryId = category.ParentId.HasValue
                    ? category.ParentId.Value.Value
                    : (Guid?)null,
                category.Name,
                category.DisplayOrder,
                category.Description
            })
            .ToArrayAsync(cancellationToken);

        var menuItems = await dbContext.MenuItems
            .AsNoTracking()
            .Where(menuItem =>
                menuItem.RestaurantId == restaurantId &&
                menuItem.IsPublished)
            .OrderBy(menuItem => menuItem.DisplayOrder)
            .ThenBy(menuItem => menuItem.Name)
            .ThenBy(menuItem => menuItem.Id)
            .Select(menuItem => new
            {
                Id = menuItem.Id.Value,
                CategoryId = menuItem.CategoryId.Value,
                menuItem.Name,
                menuItem.Description,
                PriceAmount = menuItem.Price.Amount,
                Currency = menuItem.Price.Currency,
                menuItem.DisplayOrder,
                menuItem.IsAvailable,
                menuItem.Recipe,
                menuItem.Calories,
                menuItem.Tags,
                menuItem.AllergenNotes,
                menuItem.PreparationTimeMinutes,
                menuItem.IsFeatured
            })
            .ToArrayAsync(cancellationToken);

        var menuItemsByCategory = menuItems.ToLookup(
            menuItem => menuItem.CategoryId);

        return categories
            .Select(category => new PublicMenuCategoryResponse(
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.DisplayOrder,
                menuItemsByCategory[category.Id]
                    .Select(menuItem => new PublicMenuItemResponse(
                        menuItem.Id,
                        menuItem.Name,
                        menuItem.Description,
                        menuItem.PriceAmount,
                        menuItem.Currency,
                        menuItem.DisplayOrder,
                        menuItem.IsAvailable,
                        menuItem.Recipe,
                        menuItem.Calories,
                        menuItem.Tags,
                        menuItem.AllergenNotes,
                        menuItem.PreparationTimeMinutes,
                        menuItem.IsFeatured))
                    .ToArray(),
                category.Description))
            .ToArray();
    }
}
