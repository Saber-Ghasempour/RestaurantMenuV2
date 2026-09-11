namespace RestaurantMenu.Catalog.Application.Taxation;

public sealed record BranchMenuItemTaxRuleResponse(Guid MenuItemId,
    int RateBasisPoints, string Behavior, long Version);
