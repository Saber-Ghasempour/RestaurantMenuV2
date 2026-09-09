using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.Publications;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Publications;

public sealed class BranchCatalogReadService(CatalogDbContext dbContext)
    : IBranchCatalogReadService
{
    public async Task<IReadOnlyList<BranchCategoryPublicationResponse>>
        GetConfigurationAsync(
            Guid restaurantId,
            Guid branchId,
            CancellationToken cancellationToken) =>
        await (
            from category in dbContext.MenuCategories.AsNoTracking()
            where category.RestaurantId == restaurantId
            join publication in dbContext.BranchCategoryPublications.AsNoTracking()
                .Where(value =>
                    value.RestaurantId == restaurantId &&
                    value.BranchId == branchId)
                on category.Id equals publication.CategoryId into publications
            from publication in publications.DefaultIfEmpty()
            orderby category.DisplayOrder, category.Name, category.Id
            select new BranchCategoryPublicationResponse(
                category.Id.Value,
                category.ParentId.HasValue ? category.ParentId.Value.Value : null,
                category.Name,
                category.DisplayOrder,
                publication != null && publication.IsPublished,
                publication == null ? null : publication.DisplayOrderOverride,
                publication == null ? null : publication.Version,
                category.IsPublished))
        .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<PublicMenuCategoryResponse>> GetPublicMenuAsync(
        Guid restaurantId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var categories = await (
            from category in dbContext.MenuCategories.AsNoTracking()
            join publication in dbContext.BranchCategoryPublications.AsNoTracking()
                on new { category.RestaurantId, CategoryId = category.Id }
                equals new { publication.RestaurantId, publication.CategoryId }
            where category.RestaurantId == restaurantId &&
                  category.IsPublished &&
                  publication.BranchId == branchId &&
                  publication.IsPublished
            orderby publication.DisplayOrderOverride ?? category.DisplayOrder,
                category.Name,
                category.Id
            select new
            {
                Id = category.Id.Value,
                ParentId = category.ParentId.HasValue
                    ? category.ParentId.Value.Value
                    : (Guid?)null,
                category.Name,
                DisplayOrder = publication.DisplayOrderOverride ?? category.DisplayOrder,
                category.Description
            }).ToArrayAsync(cancellationToken);

        var publishedItems =
            from item in dbContext.MenuItems.AsNoTracking()
            join publication in dbContext.BranchCategoryPublications.AsNoTracking()
                on new { item.RestaurantId, item.CategoryId }
                equals new { publication.RestaurantId, publication.CategoryId }
            where item.RestaurantId == restaurantId &&
                  item.IsPublished &&
                  publication.BranchId == branchId &&
                  publication.IsPublished
            select item;

        var items = await publishedItems
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                Id = item.Id.Value,
                CategoryId = item.CategoryId.Value,
                item.Name,
                item.Description,
                item.DisplayOrder,
                item.IsAvailable,
                item.Recipe,
                item.Calories,
                item.Tags,
                item.AllergenNotes,
                item.PreparationTimeMinutes,
                item.IsFeatured
            }).ToArrayAsync(cancellationToken);
        var itemsByCategory = items.ToLookup(item => item.CategoryId);

        var variants = await (
            from variant in dbContext.MenuItemVariants.AsNoTracking()
            join item in publishedItems
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
                Amount = variant.Price.Amount,
                Currency = variant.Price.Currency,
                variant.DisplayOrder,
                variant.IsDefault,
                variant.IsAvailable
            }).ToArrayAsync(cancellationToken);
        var variantsByItem = variants.ToLookup(variant => variant.MenuItemId);

        return categories.Select(category => new PublicMenuCategoryResponse(
            category.Id,
            category.ParentId,
            category.Name,
            category.DisplayOrder,
            itemsByCategory[category.Id].Select(item =>
            {
                var itemVariants = variantsByItem[item.Id].ToArray();
                var defaultVariant = itemVariants.Single(variant => variant.IsDefault);
                return new PublicMenuItemResponse(
                    item.Id, item.Name, item.Description, defaultVariant.Amount,
                    defaultVariant.Currency, item.DisplayOrder, item.IsAvailable,
                    item.Recipe, item.Calories, item.Tags, item.AllergenNotes,
                    item.PreparationTimeMinutes, item.IsFeatured,
                    itemVariants.Select(variant => new PublicMenuVariantResponse(
                        variant.Id, variant.Name, variant.Description,
                        variant.Amount, variant.Currency, variant.DisplayOrder,
                        variant.IsDefault, variant.IsAvailable)).ToArray());
            }).ToArray(),
            category.Description)).ToArray();
    }
}
