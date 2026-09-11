using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Taxation;

public sealed class SetBranchMenuItemTaxRuleCommandHandler(IBranchExistenceChecker branches,
    IMenuItemRepository items, IBranchMenuItemTaxRuleRepository rules,
    ICatalogUnitOfWork unitOfWork, IPublicBranchMenuCache cache, TimeProvider timeProvider)
    : ICommandHandler<SetBranchMenuItemTaxRuleCommand, Result<BranchMenuItemTaxRuleResponse>>
{
    public async Task<Result<BranchMenuItemTaxRuleResponse>> Handle(
        SetBranchMenuItemTaxRuleCommand command, CancellationToken cancellationToken)
    {
        if (!await branches.ExistsAsync(command.RestaurantId, command.BranchId, cancellationToken))
            return Result.Failure<BranchMenuItemTaxRuleResponse>(BranchMenuItemTaxRuleApplicationErrors.BranchOrItemNotFound);
        var item = await items.GetByIdAsync(new MenuItemId(command.MenuItemId), cancellationToken);
        if (item is null || item.RestaurantId != command.RestaurantId)
            return Result.Failure<BranchMenuItemTaxRuleResponse>(BranchMenuItemTaxRuleApplicationErrors.BranchOrItemNotFound);
        var rule = await rules.GetAsync(command.RestaurantId, command.BranchId,
            item.Id, cancellationToken);
        if (rule is null)
        {
            if (command.ExpectedVersion != 0)
                return Result.Failure<BranchMenuItemTaxRuleResponse>(BranchMenuItemTaxRuleApplicationErrors.VersionConflict);
            var created = BranchMenuItemTaxRule.Create(command.RestaurantId, command.BranchId,
                item.Id, command.RateBasisPoints, command.Behavior, timeProvider.GetUtcNow());
            if (created.IsFailure)
                return Result.Failure<BranchMenuItemTaxRuleResponse>(created.Error);
            rule = created.Value;
            rules.Add(rule);
        }
        else
        {
            if (rule.Version != command.ExpectedVersion)
                return Result.Failure<BranchMenuItemTaxRuleResponse>(BranchMenuItemTaxRuleApplicationErrors.VersionConflict);
            var changed = rule.Set(command.RateBasisPoints, command.Behavior);
            if (changed.IsFailure)
                return Result.Failure<BranchMenuItemTaxRuleResponse>(changed.Error);
            if (!changed.Value)
                return Result.Success(ToResponse(rule));
        }
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<BranchMenuItemTaxRuleResponse>(BranchMenuItemTaxRuleApplicationErrors.VersionConflict);
        }
        await cache.RemoveAsync(command.RestaurantId, command.BranchId, cancellationToken);
        return Result.Success(ToResponse(rule));
    }

    private static BranchMenuItemTaxRuleResponse ToResponse(BranchMenuItemTaxRule rule) =>
        new(rule.MenuItemId.Value, rule.RateBasisPoints, rule.Behavior.ToString(), rule.Version);
}

public sealed class ListBranchMenuItemTaxRulesQueryHandler(IBranchExistenceChecker branches,
    IBranchMenuItemTaxRuleRepository rules)
    : IQueryHandler<ListBranchMenuItemTaxRulesQuery,
        Result<IReadOnlyList<BranchMenuItemTaxRuleResponse>>>
{
    public async Task<Result<IReadOnlyList<BranchMenuItemTaxRuleResponse>>> Handle(
        ListBranchMenuItemTaxRulesQuery query, CancellationToken cancellationToken)
    {
        if (!await branches.ExistsAsync(query.RestaurantId, query.BranchId, cancellationToken))
            return Result.Failure<IReadOnlyList<BranchMenuItemTaxRuleResponse>>(
                BranchMenuItemTaxRuleApplicationErrors.BranchOrItemNotFound);
        var values = await rules.ListAsync(query.RestaurantId, query.BranchId, cancellationToken);
        return Result.Success<IReadOnlyList<BranchMenuItemTaxRuleResponse>>(values
            .OrderBy(rule => rule.MenuItemId.Value)
            .Select(rule => new BranchMenuItemTaxRuleResponse(rule.MenuItemId.Value,
                rule.RateBasisPoints, rule.Behavior.ToString(), rule.Version)).ToArray());
    }
}
