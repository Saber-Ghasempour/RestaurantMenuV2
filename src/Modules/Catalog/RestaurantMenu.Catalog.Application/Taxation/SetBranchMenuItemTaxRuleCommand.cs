using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Taxation;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Taxation;

public sealed record SetBranchMenuItemTaxRuleCommand(Guid RestaurantId, Guid BranchId,
    Guid MenuItemId, int RateBasisPoints, TaxBehavior Behavior, long ExpectedVersion)
    : ICommand<Result<BranchMenuItemTaxRuleResponse>>;
