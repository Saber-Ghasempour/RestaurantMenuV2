using RestaurantMenu.Catalog.Application.Abstractions.Branches;
using RestaurantMenu.Catalog.Application.Abstractions.Caching;
using RestaurantMenu.Catalog.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using RestaurantMenu.Catalog.Application.Taxation;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;

namespace RestaurantMenu.Catalog.Application.UnitTests.Taxation;

public sealed class SetBranchMenuItemTaxRuleCommandHandlerTests
{
    [Fact]
    public async Task HandleCreatesBranchItemRuleAndInvalidatesMenuAfterCommit()
    {
        var restaurantId = Guid.CreateVersion7(); var branchId = Guid.CreateVersion7();
        var item = Item(restaurantId); var rules = new RuleRepository(); var calls = new List<string>();
        var handler = Handler(item, rules, calls);

        var result = await handler.Handle(new(restaurantId, branchId, item.Id.Value,
            2300, TaxBehavior.Inclusive, 0), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2300, result.Value.RateBasisPoints);
        Assert.Equal("Inclusive", result.Value.Behavior);
        Assert.Equal(["commit", "invalidate"], calls);
        Assert.NotNull(rules.Rule);
    }

    [Fact]
    public async Task HandleRejectsMenuItemFromAnotherRestaurant()
    {
        var item = Item(Guid.CreateVersion7()); var calls = new List<string>();
        var result = await Handler(item, new RuleRepository(), calls).Handle(
            new(Guid.CreateVersion7(), Guid.CreateVersion7(), item.Id.Value,
                500, TaxBehavior.Exclusive, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Catalog.TaxRuleScopeNotFound", result.Error.Code);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task HandleRequiresExactVersionForExistingRule()
    {
        var restaurantId = Guid.CreateVersion7(); var branchId = Guid.CreateVersion7();
        var item = Item(restaurantId);
        var rule = BranchMenuItemTaxRule.Create(restaurantId, branchId, item.Id,
            1000, TaxBehavior.Exclusive, DateTimeOffset.UtcNow).Value;
        var calls = new List<string>();

        var result = await Handler(item, new RuleRepository(rule), calls).Handle(
            new(restaurantId, branchId, item.Id.Value, 2300, TaxBehavior.Inclusive, 2),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Catalog.TaxRuleVersionConflict", result.Error.Code);
        Assert.Empty(calls);
    }

    private static SetBranchMenuItemTaxRuleCommandHandler Handler(MenuItem item,
        RuleRepository rules, List<string> calls) => new(new BranchChecker(),
        new ItemRepository(item), rules, new UnitOfWork(calls), new Cache(calls), TimeProvider.System);

    private static MenuItem Item(Guid restaurantId) => MenuItem.Create(MenuItemId.New(),
        restaurantId, MenuCategoryId.New(), "Meal", null, 0, DateTimeOffset.UtcNow).Value;

    private sealed class BranchChecker : IBranchExistenceChecker
    {
        public Task<bool> ExistsAsync(Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken) => Task.FromResult(true);
    }
    private sealed class ItemRepository(MenuItem item) : IMenuItemRepository
    {
        public void Add(MenuItem menuItem) => throw new NotSupportedException();
        public Task<MenuItem?> GetByIdAsync(MenuItemId id, CancellationToken cancellationToken) =>
            Task.FromResult<MenuItem?>(id == item.Id ? item : null);
    }
    private sealed class RuleRepository(BranchMenuItemTaxRule? initial = null)
        : IBranchMenuItemTaxRuleRepository
    {
        public BranchMenuItemTaxRule? Rule { get; private set; } = initial;
        public void Add(BranchMenuItemTaxRule rule) => Rule = rule;
        public Task<BranchMenuItemTaxRule?> GetAsync(Guid restaurantId, Guid branchId,
            MenuItemId menuItemId, CancellationToken cancellationToken) => Task.FromResult(Rule);
        public Task<IReadOnlyList<BranchMenuItemTaxRule>> ListAsync(Guid restaurantId,
            Guid branchId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BranchMenuItemTaxRule>>(Rule is null ? [] : [Rule]);
    }
    private sealed class UnitOfWork(List<string> calls) : ICatalogUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { calls.Add("commit"); return Task.FromResult(1); }
    }
    private sealed class Cache(List<string> calls) : IPublicBranchMenuCache
    {
        public Task<PublicBranchMenuResponse?> GetAsync(Guid restaurantId, Guid branchId,
            CancellationToken cancellationToken) => Task.FromResult<PublicBranchMenuResponse?>(null);
        public Task SetAsync(PublicBranchMenuResponse menu, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveAsync(Guid restaurantId, Guid branchId, CancellationToken cancellationToken)
        { calls.Add("invalidate"); return Task.CompletedTask; }
    }
}
