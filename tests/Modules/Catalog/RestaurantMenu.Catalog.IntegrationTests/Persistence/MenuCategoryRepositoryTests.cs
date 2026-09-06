using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Infrastructure.Categories;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Catalog.Infrastructure.Items;

using Testcontainers.PostgreSql;

namespace RestaurantMenu.Catalog.IntegrationTests.Persistence;

public sealed class MenuCategoryRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task AddShouldPersistCategoryHierarchy()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var parentResult = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            null,
            "Main Courses",
            1,
            DateTimeOffset.UtcNow);

        Assert.True(parentResult.IsSuccess);

        var childResult = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            parentResult.Value.Id,
            "Pasta",
            2,
            DateTimeOffset.UtcNow);

        Assert.True(childResult.IsSuccess);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();

            var repository = new MenuCategoryRepository(context);
            repository.Add(parentResult.Value);
            repository.Add(childResult.Value);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            new CatalogDbContext(options);
        var repositoryForVerification =
            new MenuCategoryRepository(verificationContext);

        var persistedChild =
            await repositoryForVerification.GetByIdAsync(
                childResult.Value.Id,
                CancellationToken.None);

        Assert.NotNull(persistedChild);
        Assert.Equal(restaurantId, persistedChild.RestaurantId);
        Assert.Equal(parentResult.Value.Id, persistedChild.ParentId);
        Assert.Equal("Pasta", persistedChild.Name);
        Assert.Equal(2, persistedChild.DisplayOrder);
        Assert.Equal(1, persistedChild.Version);
    }

    [Fact]
    public async Task ReadServiceShouldFilterAndOrderRestaurantCategories()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var otherRestaurantId = Guid.CreateVersion7();
        var first = CreateCategory(
            restaurantId,
            "Appetizers",
            1);
        var second = CreateCategory(
            restaurantId,
            "Desserts",
            20);
        var other = CreateCategory(
            otherRestaurantId,
            "Hidden Category",
            0);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.MenuCategories.AddRange(
                second,
                other,
                first);
            await context.SaveChangesAsync();
        }

        await using var readContext =
            new CatalogDbContext(options);
        var readService =
            new MenuCategoryReadService(readContext);

        var categories =
            await readService.GetByRestaurantIdAsync(
                restaurantId,
                CancellationToken.None);
        var category =
            await readService.GetByIdAsync(
                restaurantId,
                second.Id,
                CancellationToken.None);
        var categoryUnderWrongRestaurant =
            await readService.GetByIdAsync(
                otherRestaurantId,
                second.Id,
                CancellationToken.None);

        Assert.Collection(
            categories,
            item => Assert.Equal(first.Id.Value, item.Id),
            item => Assert.Equal(second.Id.Value, item.Id));
        Assert.NotNull(category);
        Assert.Equal("Desserts", category.Name);
        Assert.Null(categoryUnderWrongRestaurant);
    }

    [Fact]
    public async Task SaveChangesShouldThrowWhenCategoryWasConcurrentlyUpdated()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var category = CreateCategory(
            Guid.CreateVersion7(),
            "Concurrent Category",
            1);

        await using (var arrangeContext =
                     new CatalogDbContext(options))
        {
            await arrangeContext.Database.MigrateAsync();
            arrangeContext.MenuCategories.Add(category);
            await arrangeContext.SaveChangesAsync();
        }

        await using var firstContext =
            new CatalogDbContext(options);
        await using var secondContext =
            new CatalogDbContext(options);
        var first = await firstContext.MenuCategories.SingleAsync(
            candidate => candidate.Id == category.Id);
        var second = await secondContext.MenuCategories.SingleAsync(
            candidate => candidate.Id == category.Id);

        Assert.True(first.Update(null, "First Update", 1).IsSuccess);
        Assert.True(second.Update(null, "Second Update", 1).IsSuccess);
        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletedCategoryShouldBeHiddenButRemainPersisted()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var category = CreateCategory(
            Guid.CreateVersion7(),
            "Deleted Category",
            1);
        var deletedAtUtc = new DateTimeOffset(
            2026,
            9,
            7,
            13,
            0,
            0,
            TimeSpan.Zero);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.MenuCategories.Add(category);
            await context.SaveChangesAsync();
            category.Delete(deletedAtUtc);
            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            new CatalogDbContext(options);
        Assert.Null(
            await verificationContext.MenuCategories
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == category.Id));

        var persisted =
            await verificationContext.MenuCategories
                .IgnoreQueryFilters()
                .SingleAsync(
                    candidate => candidate.Id == category.Id);
        Assert.True(persisted.IsDeleted);
        Assert.Equal(deletedAtUtc, persisted.DeletedAtUtc);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task AddShouldPersistMenuItemAndOwnedMoney()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(
            restaurantId,
            "Main Courses",
            1);
        var itemResult = MenuItem.Create(
            MenuItemId.New(),
            restaurantId,
            category.Id,
            "Carbonara",
            "Classic pasta",
            14.50m,
            "EUR",
            1,
            new DateTimeOffset(
                2026,
                9,
                7,
                16,
                0,
                0,
                TimeSpan.Zero));
        Assert.True(itemResult.IsSuccess);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.MenuCategories.Add(category);
            new MenuItemRepository(context).Add(itemResult.Value);
            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            new CatalogDbContext(options);
        var persisted = await verificationContext.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Id == itemResult.Value.Id);

        Assert.Equal(category.Id, persisted.CategoryId);
        Assert.Equal("Carbonara", persisted.Name);
        Assert.Equal("Classic pasta", persisted.Description);
        Assert.Equal(14.50m, persisted.Price.Amount);
        Assert.Equal("EUR", persisted.Price.Currency);
        Assert.True(persisted.IsAvailable);
        Assert.Equal(1, persisted.Version);
    }

    [Fact]
    public async Task MenuItemReadServiceShouldScopeProjectAndOrderItems()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, "Category", 1);
        var otherCategory = CreateCategory(
            restaurantId,
            "Other Category",
            2);
        var first = CreateMenuItem(
            restaurantId,
            category.Id,
            "Appetizer",
            1,
            8.25m);
        var second = CreateMenuItem(
            restaurantId,
            category.Id,
            "Dessert",
            20,
            6.50m);
        var excluded = CreateMenuItem(
            restaurantId,
            otherCategory.Id,
            "Excluded",
            0,
            1m);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.MenuCategories.AddRange(category, otherCategory);
            context.MenuItems.AddRange(second, excluded, first);
            await context.SaveChangesAsync();
        }

        await using var readContext = new CatalogDbContext(options);
        var readService = new MenuItemReadService(readContext);
        var items = await readService.GetByCategoryIdAsync(
            restaurantId,
            category.Id,
            CancellationToken.None);
        var item = await readService.GetByIdAsync(
            restaurantId,
            category.Id,
            second.Id,
            CancellationToken.None);
        var itemUnderWrongCategory = await readService.GetByIdAsync(
            restaurantId,
            otherCategory.Id,
            second.Id,
            CancellationToken.None);

        Assert.Collection(
            items,
            response => Assert.Equal(first.Id.Value, response.Id),
            response => Assert.Equal(second.Id.Value, response.Id));
        Assert.NotNull(item);
        Assert.Equal(6.50m, item.PriceAmount);
        Assert.Equal("EUR", item.Currency);
        Assert.Null(itemUnderWrongCategory);
    }

    [Fact]
    public async Task MenuItemRepositoryShouldLoadAndPersistOwnedMoneyUpdate()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, "Category", 1);
        var menuItem = CreateMenuItem(
            restaurantId,
            category.Id,
            "Original",
            1,
            10m);

        await using (var arrangeContext =
                     new CatalogDbContext(options))
        {
            await arrangeContext.Database.MigrateAsync();
            arrangeContext.MenuCategories.Add(category);
            arrangeContext.MenuItems.Add(menuItem);
            await arrangeContext.SaveChangesAsync();
        }

        await using (var updateContext =
                     new CatalogDbContext(options))
        {
            var repository = new MenuItemRepository(updateContext);
            var loaded = await repository.GetByIdAsync(
                menuItem.Id,
                CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.True(
                loaded.Update(
                    " Updated ",
                    " New description ",
                    19.95m,
                    "usd",
                    5).IsSuccess);
            await updateContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new CatalogDbContext(options);
        var persisted = await verificationContext.MenuItems
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == menuItem.Id);
        Assert.Equal("Updated", persisted.Name);
        Assert.Equal("New description", persisted.Description);
        Assert.Equal(19.95m, persisted.Price.Amount);
        Assert.Equal("USD", persisted.Price.Currency);
        Assert.Equal(5, persisted.DisplayOrder);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task SaveChangesShouldThrowWhenMenuItemWasConcurrentlyUpdated()
    {
        var options =
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "catalog"))
                .Options;
        var restaurantId = Guid.CreateVersion7();
        var category = CreateCategory(restaurantId, "Category", 1);
        var menuItem = CreateMenuItem(
            restaurantId,
            category.Id,
            "Concurrent Item",
            1,
            10m);

        await using (var arrangeContext =
                     new CatalogDbContext(options))
        {
            await arrangeContext.Database.MigrateAsync();
            arrangeContext.MenuCategories.Add(category);
            arrangeContext.MenuItems.Add(menuItem);
            await arrangeContext.SaveChangesAsync();
        }

        await using var firstContext =
            new CatalogDbContext(options);
        await using var secondContext =
            new CatalogDbContext(options);
        var first = await firstContext.MenuItems.SingleAsync(
            candidate => candidate.Id == menuItem.Id);
        var second = await secondContext.MenuItems.SingleAsync(
            candidate => candidate.Id == menuItem.Id);

        Assert.True(
            first.Update(
                "First Update",
                null,
                11m,
                "EUR",
                1).IsSuccess);
        Assert.True(
            second.Update(
                "Second Update",
                null,
                12m,
                "EUR",
                1).IsSuccess);
        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    private static MenuCategory CreateCategory(
        Guid restaurantId,
        string name,
        int displayOrder)
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            restaurantId,
            null,
            name,
            displayOrder,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static MenuItem CreateMenuItem(
        Guid restaurantId,
        MenuCategoryId categoryId,
        string name,
        int displayOrder,
        decimal price)
    {
        var result = MenuItem.Create(
            MenuItemId.New(),
            restaurantId,
            categoryId,
            name,
            null,
            price,
            "EUR",
            displayOrder,
            new DateTimeOffset(
                2026,
                9,
                7,
                17,
                0,
                0,
                TimeSpan.Zero));
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
