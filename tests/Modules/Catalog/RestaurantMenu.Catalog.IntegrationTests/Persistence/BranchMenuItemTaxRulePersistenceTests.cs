using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Taxation;
using RestaurantMenu.Catalog.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Catalog.IntegrationTests.Persistence;

public sealed class BranchMenuItemTaxRulePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task MigrationPersistsDifferentRatesForDifferentBranchItems()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), value =>
                value.MigrationsHistoryTable("__ef_migrations_history", "catalog")).Options;
        var restaurantId = Guid.CreateVersion7(); var branchId = Guid.CreateVersion7();
        var category = MenuCategory.Create(MenuCategoryId.New(), restaurantId, null,
            "Food", 0, DateTimeOffset.UtcNow).Value;
        var first = MenuItem.Create(MenuItemId.New(), restaurantId, category.Id,
            "Meal", null, 0, DateTimeOffset.UtcNow).Value;
        var second = MenuItem.Create(MenuItemId.New(), restaurantId, category.Id,
            "Drink", null, 1, DateTimeOffset.UtcNow).Value;

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            Assert.False(context.Database.HasPendingModelChanges());
            context.MenuCategories.Add(category); context.MenuItems.AddRange(first, second);
            context.BranchMenuItemTaxRules.AddRange(
                BranchMenuItemTaxRule.Create(restaurantId, branchId, first.Id,
                    2300, TaxBehavior.Inclusive, DateTimeOffset.UtcNow).Value,
                BranchMenuItemTaxRule.Create(restaurantId, branchId, second.Id,
                    1300, TaxBehavior.Exclusive, DateTimeOffset.UtcNow).Value);
            await context.SaveChangesAsync();
        }

        await using var verify = new CatalogDbContext(options);
        var saved = await verify.BranchMenuItemTaxRules.AsNoTracking()
            .OrderBy(rule => rule.RateBasisPoints).ToArrayAsync();
        Assert.Equal([1300, 2300], saved.Select(rule => rule.RateBasisPoints));
        Assert.Equal([TaxBehavior.Exclusive, TaxBehavior.Inclusive],
            saved.Select(rule => rule.Behavior));
    }
}
