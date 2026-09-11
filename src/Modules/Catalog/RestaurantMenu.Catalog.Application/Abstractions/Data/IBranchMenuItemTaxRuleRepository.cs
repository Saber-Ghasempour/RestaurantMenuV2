using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IBranchMenuItemTaxRuleRepository
{
    void Add(BranchMenuItemTaxRule rule);
    Task<BranchMenuItemTaxRule?> GetAsync(Guid restaurantId, Guid branchId,
        MenuItemId menuItemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BranchMenuItemTaxRule>> ListAsync(Guid restaurantId,
        Guid branchId, CancellationToken cancellationToken);
}
