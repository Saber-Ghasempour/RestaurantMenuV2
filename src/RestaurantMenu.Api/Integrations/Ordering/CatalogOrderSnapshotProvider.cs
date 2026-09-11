using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Ordering.Application.Abstractions;
using OrderingTaxBehavior = RestaurantMenu.Ordering.Domain.Orders.TaxBehavior;
using CatalogTaxBehavior = RestaurantMenu.Catalog.Domain.Taxation.TaxBehavior;

namespace RestaurantMenu.Api.Integrations.Ordering;

public sealed class CatalogOrderSnapshotProvider(CatalogDbContext dbContext)
    : ICatalogOrderSnapshotProvider
{
    public async Task<IReadOnlyList<CatalogOrderLineSnapshot>?> GetOrderableAsync(
        Guid restaurantId, Guid branchId, IReadOnlyCollection<CatalogOrderLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var itemIds = lines.Select(line => line.MenuItemId).Distinct().ToArray();
        var requestedItems = dbContext.MenuItems.AsNoTracking()
            .Where(BuildItemPredicate(itemIds));
        var candidates = await (
            from item in requestedItems
            join category in dbContext.MenuCategories.AsNoTracking()
                on new { item.RestaurantId, CategoryId = item.CategoryId }
                equals new { category.RestaurantId, CategoryId = category.Id }
            join publication in dbContext.BranchCategoryPublications.AsNoTracking()
                on new { item.RestaurantId, item.CategoryId }
                equals new { publication.RestaurantId, publication.CategoryId }
            join variant in dbContext.MenuItemVariants.AsNoTracking()
                on new { item.RestaurantId, MenuItemId = item.Id }
                equals new { variant.RestaurantId, MenuItemId = variant.MenuItemId }
            where item.RestaurantId == restaurantId &&
                  item.IsPublished && item.IsAvailable && category.IsPublished &&
                  publication.BranchId == branchId && publication.IsPublished &&
                  variant.IsAvailable
            join taxRule in dbContext.BranchMenuItemTaxRules.AsNoTracking()
                on new { item.RestaurantId, BranchId = publication.BranchId, MenuItemId = item.Id }
                equals new { taxRule.RestaurantId, taxRule.BranchId, taxRule.MenuItemId }
                into taxRules
            from taxRule in taxRules.DefaultIfEmpty()
            select new Candidate(item.Id.Value, variant.Id.Value, item.Name, variant.Name,
                variant.Price.Amount, variant.Price.Currency, variant.IsDefault,
                taxRule == null ? 0 : taxRule.RateBasisPoints,
                taxRule == null ? CatalogTaxBehavior.Exclusive : taxRule.Behavior))
            .ToArrayAsync(cancellationToken);

        var result = new List<CatalogOrderLineSnapshot>(lines.Count);
        foreach (var requested in lines)
        {
            var candidate = requested.VariantId is { } variantId
                ? candidates.SingleOrDefault(value => value.ItemId == requested.MenuItemId && value.VariantId == variantId)
                : candidates.SingleOrDefault(value => value.ItemId == requested.MenuItemId && value.IsDefault);
            if (candidate is null) return null;
            result.Add(new CatalogOrderLineSnapshot(candidate.ItemId, candidate.VariantId,
                candidate.ItemName, candidate.VariantName, candidate.Amount, candidate.Currency,
                candidate.TaxRateBasisPoints, candidate.TaxBehavior == CatalogTaxBehavior.Inclusive
                    ? OrderingTaxBehavior.Inclusive : OrderingTaxBehavior.Exclusive));
        }
        return result;
    }

    private sealed record Candidate(Guid ItemId, Guid VariantId, string ItemName,
        string VariantName, decimal Amount, string Currency, bool IsDefault,
        int TaxRateBasisPoints, CatalogTaxBehavior TaxBehavior);

    private static Expression<Func<MenuItem, bool>> BuildItemPredicate(IReadOnlyList<Guid> itemIds)
    {
        var item = Expression.Parameter(typeof(MenuItem), "item");
        var id = Expression.Property(item, nameof(MenuItem.Id));
        Expression body = Expression.Constant(false);
        foreach (var itemId in itemIds)
            body = Expression.OrElse(body, Expression.Equal(id,
                Expression.Constant(new MenuItemId(itemId))));
        return Expression.Lambda<Func<MenuItem, bool>>(body, item);
    }
}
