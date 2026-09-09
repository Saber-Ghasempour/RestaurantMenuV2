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

        var visibleItems = dbContext.MenuItems
            .AsNoTracking()
            .Where(menuItem =>
                menuItem.RestaurantId == restaurantId &&
                menuItem.IsPublished);

        var menuItems = await visibleItems
            .OrderBy(menuItem => menuItem.DisplayOrder)
            .ThenBy(menuItem => menuItem.Name)
            .ThenBy(menuItem => menuItem.Id)
            .Select(menuItem => new
            {
                Id = menuItem.Id.Value,
                CategoryId = menuItem.CategoryId.Value,
                menuItem.Name,
                menuItem.Description,
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

        var variants = await (
            from variant in dbContext.MenuItemVariants.AsNoTracking()
            join item in visibleItems
                on new { variant.RestaurantId, variant.MenuItemId }
                equals new { item.RestaurantId, MenuItemId = item.Id }
            orderby variant.IsDefault descending, variant.DisplayOrder,
                variant.Name, variant.Id
            select new
            {
                Id = variant.Id.Value,
                MenuItemId = variant.MenuItemId.Value,
                variant.Name,
                variant.Description,
                PriceAmount = variant.Price.Amount,
                Currency = variant.Price.Currency,
                variant.DisplayOrder,
                variant.IsDefault,
                variant.IsAvailable
            })
            .ToArrayAsync(cancellationToken);

        var menuItemsByCategory = menuItems.ToLookup(
            menuItem => menuItem.CategoryId);
        var variantsByItem = variants.ToLookup(variant => variant.MenuItemId);

        return categories
            .Select(category => new PublicMenuCategoryResponse(
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.DisplayOrder,
                menuItemsByCategory[category.Id]
                    .Select(menuItem =>
                    {
                        var itemVariants = variantsByItem[menuItem.Id].ToArray();
                        var defaultVariant = itemVariants.Single(variant => variant.IsDefault);
                        return new PublicMenuItemResponse(
                            menuItem.Id, menuItem.Name, menuItem.Description,
                            defaultVariant.PriceAmount, defaultVariant.Currency,
                            menuItem.DisplayOrder, menuItem.IsAvailable,
                            menuItem.Recipe, menuItem.Calories, menuItem.Tags,
                            menuItem.AllergenNotes, menuItem.PreparationTimeMinutes,
                            menuItem.IsFeatured,
                            itemVariants.Select(variant => new PublicMenuVariantResponse(
                                variant.Id, variant.Name, variant.Description,
                                variant.PriceAmount, variant.Currency,
                                variant.DisplayOrder, variant.IsDefault,
                                variant.IsAvailable)).ToArray());
                    })
                    .ToArray(),
                category.Description))
            .ToArray();
    }
}
