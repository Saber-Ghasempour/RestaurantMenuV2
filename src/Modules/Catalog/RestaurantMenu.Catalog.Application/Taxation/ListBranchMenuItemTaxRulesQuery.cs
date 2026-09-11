using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Taxation;

public sealed record ListBranchMenuItemTaxRulesQuery(Guid RestaurantId, Guid BranchId)
    : IQuery<Result<IReadOnlyList<BranchMenuItemTaxRuleResponse>>>;
