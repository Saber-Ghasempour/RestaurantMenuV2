using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.Catalog.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Catalog.IntegrationTests.Persistence;

public sealed class MenuItemVariantPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task MigrationShouldBackfillEveryLegacyPriceBeforeDroppingColumns()
    {
        var options = CreateOptions();
        var restaurantId = Guid.CreateVersion7();
        var categoryId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        await using var context = new CatalogDbContext(options);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260908203754_AddCatalogMetadataAndPublication");
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO catalog.menu_categories
                (id, restaurant_id, name, display_order, created_at_utc)
            VALUES ({categoryId}, {restaurantId}, {'C' + "ategory"}, 1, {DateTimeOffset.UtcNow});
            INSERT INTO catalog.menu_items
                (id, restaurant_id, category_id, name, price_amount,
                 price_currency, display_order, created_at_utc)
            VALUES ({itemId}, {restaurantId}, {categoryId}, {'I' + "tem"},
                    {12.34m}, {"EUR"}, 1, {DateTimeOffset.UtcNow});
            """);

        await migrator.MigrateAsync();
        context.ChangeTracker.Clear();

        var variant = await context.MenuItemVariants.AsNoTracking().SingleAsync();
        Assert.Equal(itemId, variant.MenuItemId.Value);
        Assert.Equal("Default", variant.Name);
        Assert.Equal(12.34m, variant.Price.Amount);
        Assert.Equal("EUR", variant.Price.Currency);
        Assert.True(variant.IsDefault);
    }

    [Fact]
    public async Task DatabaseShouldRejectTwoActiveDefaultsAndCrossTenantParent()
    {
        var options = CreateOptions();
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId);
        var item = CreateItem(restaurantId, category.Id);
        await using (var arrange = new CatalogDbContext(options))
        {
            await arrange.Database.MigrateAsync();
            arrange.MenuCategories.Add(category);
            arrange.MenuItems.Add(item);
            await arrange.SaveChangesAsync();
        }

        await using (var duplicate = new CatalogDbContext(options))
        {
            duplicate.MenuItemVariants.AddRange(
                CreateVariant(restaurantId, item.Id, "One", true),
                CreateVariant(restaurantId, item.Id, "Two", true));
            await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicate.SaveChangesAsync());
        }

        await using var foreign = new CatalogDbContext(options);
        foreign.MenuItemVariants.Add(CreateVariant(
            Guid.CreateVersion7(), item.Id, "Foreign", true));
        await Assert.ThrowsAsync<DbUpdateException>(
            () => foreign.SaveChangesAsync());
    }

    [Fact]
    public async Task VariantVersionShouldRejectConcurrentUpdate()
    {
        var options = CreateOptions();
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId);
        var item = CreateItem(restaurantId, category.Id);
        var variant = CreateVariant(restaurantId, item.Id, "Default", true);
        await using (var arrange = new CatalogDbContext(options))
        {
            await arrange.Database.MigrateAsync();
            arrange.MenuCategories.Add(category);
            arrange.MenuItems.Add(item);
            arrange.MenuItemVariants.Add(variant);
            await arrange.SaveChangesAsync();
        }

        await using var firstContext = new CatalogDbContext(options);
        await using var secondContext = new CatalogDbContext(options);
        var first = await firstContext.MenuItemVariants.SingleAsync();
        var second = await secondContext.MenuItemVariants.SingleAsync();
        first.ChangeAvailability(false);
        second.ChangeAvailability(false);
        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    private DbContextOptions<CatalogDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), builder =>
                builder.MigrationsHistoryTable("__ef_migrations_history", "catalog"))
            .Options;

    private static MenuCategory CreateCategory(Guid restaurantId) =>
        MenuCategory.Create(MenuCategoryId.New(), restaurantId, null,
            "Category", 1, DateTimeOffset.UtcNow).Value;

    private static MenuItem CreateItem(Guid restaurantId, MenuCategoryId categoryId) =>
        MenuItem.Create(MenuItemId.New(), restaurantId, categoryId,
            "Item", null, 1, DateTimeOffset.UtcNow).Value;

    private static MenuItemVariant CreateVariant(
        Guid restaurantId, MenuItemId itemId, string name, bool isDefault) =>
        MenuItemVariant.Create(MenuItemVariantId.New(), restaurantId, itemId,
            name, null, 10m, "EUR", 0, isDefault, DateTimeOffset.UtcNow).Value;
}
