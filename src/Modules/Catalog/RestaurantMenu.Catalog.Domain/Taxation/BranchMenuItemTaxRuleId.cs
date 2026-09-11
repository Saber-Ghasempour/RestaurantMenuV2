using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Domain.Taxation;

public readonly record struct BranchMenuItemTaxRuleId(Guid BranchId, MenuItemId MenuItemId);
