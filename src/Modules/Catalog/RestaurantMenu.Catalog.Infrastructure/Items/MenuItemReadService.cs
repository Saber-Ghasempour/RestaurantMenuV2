using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Items;

public sealed class MenuItemReadService(CatalogDbContext dbContext)
    : IMenuItemReadService
{
    public async Task<MenuItemResponse?> GetByIdAsync(
        Guid restaurantId, MenuCategoryId categoryId, MenuItemId menuItemId,
        CancellationToken cancellationToken)
    {
        var items = await ReadItemsAsync(
            restaurantId, categoryId, menuItemId, cancellationToken);
        return items.SingleOrDefault();
    }

    public Task<IReadOnlyList<MenuItemResponse>> GetByCategoryIdAsync(
        Guid restaurantId, MenuCategoryId categoryId,
        CancellationToken cancellationToken) =>
        ReadItemsAsync(restaurantId, categoryId, null, cancellationToken);

    private async Task<IReadOnlyList<MenuItemResponse>> ReadItemsAsync(
        Guid restaurantId, MenuCategoryId categoryId, MenuItemId? menuItemId,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.MenuItems.AsNoTracking()
            .Where(item => item.RestaurantId == restaurantId &&
                item.CategoryId == categoryId &&
                (!menuItemId.HasValue || item.Id == menuItemId.Value))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                Id = item.Id.Value, item.RestaurantId,
                CategoryId = item.CategoryId.Value, item.Name, item.Description,
                item.DisplayOrder, item.IsAvailable, item.CreatedAtUtc,
                item.Version, item.Recipe, item.Calories, item.Tags,
                item.AllergenNotes, item.PreparationTimeMinutes,
                item.IsFeatured, item.IsPublished
            }).ToArrayAsync(cancellationToken);

        var variants = await (
            from variant in dbContext.MenuItemVariants.AsNoTracking()
            join item in dbContext.MenuItems.AsNoTracking()
                on new { variant.RestaurantId, variant.MenuItemId }
                equals new { item.RestaurantId, MenuItemId = item.Id }
            where item.RestaurantId == restaurantId &&
                  item.CategoryId == categoryId &&
                  (!menuItemId.HasValue || item.Id == menuItemId.Value)
            orderby variant.IsDefault descending, variant.DisplayOrder,
                variant.Name, variant.Id
            select new MenuItemVariantResponse(
                variant.Id.Value, variant.RestaurantId, variant.MenuItemId.Value,
                variant.Name, variant.Description, variant.Price.Amount,
                variant.Price.Currency, variant.DisplayOrder, variant.IsDefault,
                variant.IsAvailable, variant.CreatedAtUtc, variant.Version))
            .ToArrayAsync(cancellationToken);
        var byItem = variants.ToLookup(variant => variant.MenuItemId);
        var media = await dbContext.MenuItemMedia.AsNoTracking()
            .Where(x => x.RestaurantId == restaurantId && (!menuItemId.HasValue || x.MenuItemId == menuItemId.Value))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.MediaAssetId)
            .Select(x => new { MenuItemId = x.MenuItemId.Value,
                Value = new MenuItemMediaResponse(x.MediaAssetId, x.DisplayOrder, x.AltText, x.IsPrimary) })
            .ToArrayAsync(cancellationToken);
        var mediaByItem = media.ToLookup(x => x.MenuItemId, x => x.Value);

        return items.Select(item =>
        {
            var itemVariants = byItem[item.Id].ToArray();
            var defaultVariant = itemVariants.Single(variant => variant.IsDefault);
            return new MenuItemResponse(
                item.Id, item.RestaurantId, item.CategoryId, item.Name,
                item.Description, defaultVariant.PriceAmount,
                defaultVariant.Currency, item.DisplayOrder, item.IsAvailable,
                item.CreatedAtUtc, item.Version, item.Recipe, item.Calories,
                item.Tags, item.AllergenNotes, item.PreparationTimeMinutes,
                item.IsFeatured, item.IsPublished, itemVariants, mediaByItem[item.Id].ToArray());
        }).ToArray();
    }
}
