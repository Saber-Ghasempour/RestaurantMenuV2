using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Taxation;

public static class BranchMenuItemTaxRuleApplicationErrors
{
    public static readonly ErrorDetail BranchOrItemNotFound = ErrorDetail.NotFound(
        "Catalog.TaxRuleScopeNotFound", "The branch or menu item was not found.");
    public static readonly ErrorDetail VersionConflict = ErrorDetail.Conflict(
        "Catalog.TaxRuleVersionConflict", "The tax rule changed. Reload it and try again.");
}
