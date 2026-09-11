using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Taxation;

public static class BranchMenuItemTaxRuleErrors
{
    public static readonly ErrorDetail InvalidScope = ErrorDetail.Validation(
        "Catalog.InvalidTaxScope", "The restaurant, branch, and menu item are required.");
    public static readonly ErrorDetail InvalidTaxRate = ErrorDetail.Validation(
        "Catalog.InvalidTaxRate", "The tax rate must be between 0 and 10,000 basis points.");
    public static readonly ErrorDetail InvalidTaxBehavior = ErrorDetail.Validation(
        "Catalog.InvalidTaxBehavior", "The tax behavior must be Inclusive or Exclusive.");
}
