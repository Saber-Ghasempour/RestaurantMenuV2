using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Publications;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Catalog.Infrastructure.Publications;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Catalog.IntegrationTests.Persistence;

public sealed class BranchCategoryPublicationPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task PublicProjectionShouldExcludeUnpublishedAndUseOverrideOrdering()
    {
        var options = CreateOptions();
        var restaurantId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var first = CreateCategory(restaurantId, "First", 1);
        var second = CreateCategory(restaurantId, "Second", 2);
        var hidden = CreateCategory(restaurantId, "Hidden", 0);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.MenuCategories.AddRange(first, second, hidden);
            context.MenuItems.AddRange(
                CreateItem(restaurantId, first.Id, "First item"),
                CreateItem(restaurantId, second.Id, "Second item"),
                CreateItem(restaurantId, hidden.Id, "Hidden item"));
            context.BranchCategoryPublications.AddRange(
                CreatePublication(restaurantId, branchId, first.Id, true, 20),
                CreatePublication(restaurantId, branchId, second.Id, true, 10),
                CreatePublication(restaurantId, branchId, hidden.Id, false, 0));
            await context.SaveChangesAsync();
        }

        await using var readContext = new CatalogDbContext(options);
        var service = new BranchCatalogReadService(readContext);
        var result = await service.GetPublicMenuAsync(
            restaurantId, branchId, CancellationToken.None);

        Assert.Equal(["Second", "First"], result.Select(category => category.Name));
        Assert.All(result, category => Assert.Single(category.Items));
        Assert.DoesNotContain(result, category => category.Id == hidden.Id.Value);
    }

    [Fact]
    public async Task CompositeCategoryOwnershipShouldRejectCrossTenantPublication()
    {
        var options = CreateOptions();
        var category = CreateCategory(Guid.CreateVersion7(), "Other tenant", 1);

        await using var context = new CatalogDbContext(options);
        await context.Database.MigrateAsync();
        context.MenuCategories.Add(category);
        await context.SaveChangesAsync();
        context.BranchCategoryPublications.Add(
            CreatePublication(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                category.Id,
                true,
                null));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    private DbContextOptions<CatalogDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                options => options.MigrationsHistoryTable(
                    "__ef_migrations_history", "catalog"))
            .Options;

    private static MenuCategory CreateCategory(
        Guid restaurantId, string name, int order) =>
        MenuCategory.Create(
            MenuCategoryId.New(), restaurantId, null, name, order,
            DateTimeOffset.UtcNow).Value;

    private static MenuItem CreateItem(
        Guid restaurantId, MenuCategoryId categoryId, string name) =>
        MenuItem.Create(
            MenuItemId.New(), restaurantId, categoryId, name, null,
            10m, "EUR", 1, DateTimeOffset.UtcNow).Value;

    private static BranchCategoryPublication CreatePublication(
        Guid restaurantId,
        Guid branchId,
        MenuCategoryId categoryId,
        bool published,
        int? order) =>
        BranchCategoryPublication.Create(
            restaurantId, branchId, categoryId, published, order,
            DateTimeOffset.UtcNow).Value;
}
