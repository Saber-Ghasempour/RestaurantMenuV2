using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Taxation;

public sealed class BranchMenuItemTaxRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TaxBehavior.Inclusive)]
    [InlineData(TaxBehavior.Exclusive)]
    public void CreateSnapshotsBranchItemRateAndBehavior(TaxBehavior behavior)
    {
        var restaurantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var itemId = MenuItemId.New();

        var result = BranchMenuItemTaxRule.Create(
            restaurantId, branchId, itemId, 2_300, behavior, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal(branchId, result.Value.BranchId);
        Assert.Equal(itemId, result.Value.MenuItemId);
        Assert.Equal(2_300, result.Value.RateBasisPoints);
        Assert.Equal(behavior, result.Value.Behavior);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void CreateRejectsInvalidTaxRate(int rateBasisPoints)
    {
        var result = BranchMenuItemTaxRule.Create(Guid.NewGuid(), Guid.NewGuid(),
            MenuItemId.New(), rateBasisPoints, TaxBehavior.Exclusive, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("Catalog.InvalidTaxRate", result.Error.Code);
    }
}
