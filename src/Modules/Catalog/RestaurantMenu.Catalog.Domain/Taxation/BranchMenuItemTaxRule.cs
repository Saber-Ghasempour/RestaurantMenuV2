using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Taxation;

public sealed class BranchMenuItemTaxRule : AggregateRoot<BranchMenuItemTaxRuleId>
{
    private BranchMenuItemTaxRule() : base(default) { }

    private BranchMenuItemTaxRule(Guid restaurantId, Guid branchId, MenuItemId menuItemId,
        int rateBasisPoints, TaxBehavior behavior, DateTimeOffset createdAtUtc)
        : base(new BranchMenuItemTaxRuleId(branchId, menuItemId))
    {
        RestaurantId = restaurantId;
        BranchId = branchId;
        MenuItemId = menuItemId;
        RateBasisPoints = rateBasisPoints;
        Behavior = behavior;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RestaurantId { get; }
    public Guid BranchId { get; }
    public MenuItemId MenuItemId { get; }
    public int RateBasisPoints { get; private set; }
    public TaxBehavior Behavior { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public long Version { get; private set; } = 1;

    public static Result<BranchMenuItemTaxRule> Create(Guid restaurantId, Guid branchId,
        MenuItemId menuItemId, int rateBasisPoints, TaxBehavior behavior,
        DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty || branchId == Guid.Empty || menuItemId.Value == Guid.Empty)
            return Result.Failure<BranchMenuItemTaxRule>(BranchMenuItemTaxRuleErrors.InvalidScope);
        if (rateBasisPoints is < 0 or > 10_000)
            return Result.Failure<BranchMenuItemTaxRule>(BranchMenuItemTaxRuleErrors.InvalidTaxRate);
        if (!Enum.IsDefined(behavior))
            return Result.Failure<BranchMenuItemTaxRule>(BranchMenuItemTaxRuleErrors.InvalidTaxBehavior);
        return Result.Success(new BranchMenuItemTaxRule(restaurantId, branchId, menuItemId,
            rateBasisPoints, behavior, createdAtUtc));
    }

    public Result<bool> Set(int rateBasisPoints, TaxBehavior behavior)
    {
        if (rateBasisPoints is < 0 or > 10_000)
            return Result.Failure<bool>(BranchMenuItemTaxRuleErrors.InvalidTaxRate);
        if (!Enum.IsDefined(behavior))
            return Result.Failure<bool>(BranchMenuItemTaxRuleErrors.InvalidTaxBehavior);
        if (RateBasisPoints == rateBasisPoints && Behavior == behavior)
            return Result.Success(false);
        RateBasisPoints = rateBasisPoints;
        Behavior = behavior;
        Version++;
        return Result.Success(true);
    }
}
