using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;
using RestaurantMenu.Catalog.Infrastructure.Database;

namespace RestaurantMenu.Catalog.Infrastructure.Taxation;

public sealed class BranchMenuItemTaxRuleRepository(CatalogDbContext dbContext)
    : IBranchMenuItemTaxRuleRepository
{
    public void Add(BranchMenuItemTaxRule rule) => dbContext.BranchMenuItemTaxRules.Add(rule);

    public Task<BranchMenuItemTaxRule?> GetAsync(Guid restaurantId, Guid branchId,
        MenuItemId menuItemId, CancellationToken cancellationToken) =>
        dbContext.BranchMenuItemTaxRules.SingleOrDefaultAsync(rule =>
            rule.RestaurantId == restaurantId && rule.BranchId == branchId &&
            rule.MenuItemId == menuItemId, cancellationToken);

    public async Task<IReadOnlyList<BranchMenuItemTaxRule>> ListAsync(Guid restaurantId,
        Guid branchId, CancellationToken cancellationToken) =>
        await dbContext.BranchMenuItemTaxRules.AsNoTracking().Where(rule =>
            rule.RestaurantId == restaurantId && rule.BranchId == branchId)
            .ToArrayAsync(cancellationToken);
}
