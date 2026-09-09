using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.Catalog.Infrastructure.Database;
using RestaurantMenu.Catalog.Infrastructure.Items;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Catalog.IntegrationTests.Persistence;

public sealed class CatalogMetadataPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task MetadataShouldRoundTripThroughManagementProjection()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), builder =>
                builder.MigrationsHistoryTable("__ef_migrations_history", "catalog"))
            .Options;
        var restaurantId = Guid.CreateVersion7();
        var category = MenuCategory.Create(
            MenuCategoryId.New(), restaurantId, null, "Soups", 1,
            DateTimeOffset.UtcNow).Value;
        category.UpdateContent("Seasonal");
        category.ChangePublication(true);
        var item = MenuItem.Create(
            MenuItemId.New(), restaurantId, category.Id, "Tomato", null,
            1, DateTimeOffset.UtcNow).Value;
        var variant = MenuItemVariant.Create(
            MenuItemVariantId.New(), restaurantId, item.Id, "Default", null,
            8m, "EUR", 0, true, DateTimeOffset.UtcNow).Value;
        item.UpdateMetadata("Tomatoes", 0, ["vegan", "quick"], "Nuts", 15, true);
        item.ChangePublication(true);

        await using (var context = new CatalogDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.MenuCategories.Add(category);
            context.MenuItems.Add(item);
            context.MenuItemVariants.Add(variant);
            await context.SaveChangesAsync();
        }

        await using var readContext = new CatalogDbContext(options);
        var response = await new MenuItemReadService(readContext).GetByIdAsync(
            restaurantId, category.Id, item.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Tomatoes", response.Recipe);
        Assert.Equal(0, response.Calories);
        Assert.Equal(["quick", "vegan"], response.Tags);
        Assert.Equal("Nuts", response.AllergenNotes);
        Assert.Equal((short)15, response.PreparationTimeMinutes);
        Assert.True(response.IsFeatured);
        Assert.True(response.IsPublished);
    }
}
